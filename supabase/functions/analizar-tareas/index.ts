import { createClient } from "https://esm.sh/@supabase/supabase-js@2";

const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Headers": "authorization, x-client-info, apikey, content-type",
  "Access-Control-Allow-Methods": "POST, OPTIONS",
};

Deno.serve(async (req) => {
  // CORS preflight
  if (req.method === "OPTIONS") {
    return new Response("ok", { headers: corsHeaders });
  }

  try {
    // 1. Leer el token JWT del usuario autenticado
    const authHeader = req.headers.get("Authorization") ?? "";
    const jwt = authHeader.replace("Bearer ", "");
    if (!jwt) {
      return json({ error: "Sesión no autenticada." }, 401);
    }

    // 2. Cliente Supabase con permisos del usuario
    const supabase = createClient(
      Deno.env.get("SUPABASE_URL")!,
      Deno.env.get("SUPABASE_ANON_KEY")!,
      { global: { headers: { Authorization: `Bearer ${jwt}` } } },
    );

    // 3. Obtener el perfil del usuario desde el JWT
    const { data: { user }, error: errUser } = await supabase.auth.getUser(jwt);
    if (errUser || !user) {
      return json({ error: "Token inválido o expirado." }, 401);
    }

    // 4. Buscar el id_usuario en la tabla usuario (columna auth_user_id)
    const { data: perfil, error: errPerfil } = await supabase
      .from("usuario")
      .select("id_usuario, nombre")
      .eq("auth_user_id", user.id)
      .single();

    if (errPerfil || !perfil) {
      return json({ error: "No se encontró el perfil del usuario." }, 404);
    }

    // 5. Cargar las tareas del usuario
    const { data: tareas, error: errTareas } = await supabase
      .from("tarea")
      .select("titulo, descripcion, fecha_vencimiento, estado")
      .eq("id_usuario", perfil.id_usuario)
      .order("fecha_vencimiento", { ascending: true, nullsFirst: false });

    if (errTareas) {
      return json({ error: `Error al leer tareas: ${errTareas.message}` }, 500);
    }

    // 6. Llamar al LLM si hay API key; si no, fallback heurístico
    const pendientes = (tareas ?? []).filter((t) => t.estado !== "Completado");
    const plan = await generarPlan(pendientes);

    return json({
      nombre: perfil.nombre,
      pendientes: plan.pendientes,
      plan: plan.plan,
      prioridades: plan.prioridades,
    });
  } catch (e) {
    return json({ error: `Error interno: ${e.message}` }, 500);
  }
});

async function generarPlan(tareas: Array<{
  titulo: string;
  descripcion?: string | null;
  fecha_vencimiento?: string | null;
  estado?: string | null;
}>) {
  const openaiKey = Deno.env.get("OPENAI_API_KEY");
  const modeloOpenAi = Deno.env.get("OPENAI_MODEL") ?? "gpt-4o-mini";

  const systemPrompt =
    "Eres un agente de productividad experto. Dada una lista de tareas con su fecha de vencimiento, " +
    "responde SIEMPRE con JSON válido con este esquema: " +
    "{\"plan\": \"plan de acción claro y ordenado en español\", \"prioridades\": [{\"titulo\": \"...\", \"razon\": \"...\"}]}. " +
    "Reglas de organización:\n" +
    "1. Ordena las prioridades por urgencia real usando fecha_vencimiento: vencidas primero, hoy, luego esta semana, luego próximas, y sin fecha al final.\n" +
    "2. Máximo 5 prioridades, cada una con una razón breve que mencione su fecha real.\n" +
    "3. En el plan, agrupa tareas por categoría temporal (Vencidas / Hoy / Esta semana / Después).\n" +
    "Usa SIEMPRE las fechas_vencimiento que recibes; no las inventes ni las ignores.";

  const userContent = `Estas son mis tareas pendientes (ya ordenadas por fecha, incluye el vencimiento exacto):\n${JSON.stringify(tareas)}`;

  // 1) OpenAI
  if (openaiKey) {
    try {
      const res = await fetch("https://api.openai.com/v1/chat/completions", {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${openaiKey}`,
        },
        body: JSON.stringify({
          model: modeloOpenAi,
          temperature: 0.4,
          messages: [
            { role: "system", content: systemPrompt },
            { role: "user", content: userContent },
          ],
          response_format: { type: "json_object" },
        }),
      });

      const data = await res.json();
      const contenido = data.choices?.[0]?.message?.content;
      if (contenido) {
        const parsed = JSON.parse(contenido);
        return {
          pendientes: tareas.length,
          plan: parsed.plan ?? "Pude analizar tus tareas.",
          prioridades: parsed.prioridades ?? [],
        };
      }
      throw new Error("El modelo no devolvió contenido.");
    } catch (e) {
      return {
        pendientes: tareas.length,
        plan: `No pude contactar al modelo de IA (${e.message}). Uso priorización automática por fecha.`,
        prioridades: heuristica(tareas),
      };
    }
  }

  if (tareas.length === 0) {
    return {
      pendientes: 0,
      plan: "No tienes tareas pendientes. Crea algunas primero.",
      prioridades: [],
    };
  }

  return {
    pendientes: tareas.length,
    plan: planHeuristico(tareas),
    prioridades: heuristica(tareas),
  };
}

function planHeuristico(tareas: Array<{
  titulo: string;
  fecha_vencimiento?: string | null;
}>) {
  const hoyInicio = new Date();
  hoyInicio.setHours(0, 0, 0, 0);
  const hoyFin = new Date(hoyInicio);
  hoyFin.setDate(hoyFin.getDate() + 1);
  const semanaFin = new Date(hoyInicio);
  semanaFin.setDate(semanaFin.getDate() + 7);

  const grupos: Record<string, string[]> = { Vencidas: [], "Hoy": [], "Esta semana": [], "Después": [], "Sin fecha": [] };

  for (const t of tareas) {
    const f = t.fecha_vencimiento ? new Date(t.fecha_vencimiento) : null;

    if (!f) {
      grupos["Sin fecha"].push(t.titulo);
    } else if (f < hoyInicio) {
      grupos["Vencidas"].push(t.titulo);
    } else if (f >= hoyInicio && f < hoyFin) {
      grupos["Hoy"].push(t.titulo);
    } else if (f >= hoyFin && f < semanaFin) {
      grupos["Esta semana"].push(t.titulo);
    } else {
      grupos["Después"].push(t.titulo);
    }
  }

  const lineas: string[] = [];
  for (const nombre of ["Vencidas", "Hoy", "Esta semana", "Después", "Sin fecha"]) {
    if (grupos[nombre].length > 0) {
      lineas.push(`${nombre}: ${grupos[nombre].join(", ")}`);
    }
  }
  return "Plan organizado por urgencia:\n" + lineas.join("\n");
}

function heuristica(tareas: Array<{
  titulo: string;
  descripcion?: string | null;
  fecha_vencimiento?: string | null;
  estado?: string | null;
}>) {
  const ordenadas = [...tareas].sort((a, b) => {
    if (!a.fecha_vencimiento) return 1;
    if (!b.fecha_vencimiento) return -1;
    return new Date(a.fecha_vencimiento) - new Date(b.fecha_vencimiento);
  });

  return ordenadas.slice(0, 5).map((t) => ({
    titulo: t.titulo,
    razon: t.fecha_vencimiento
      ? `Vence el ${new Date(t.fecha_vencimiento).toLocaleDateString("es-ES")}`
      : "Sin fecha de vencimiento",
  }));
}

function json(obj: unknown, status = 200) {
  return new Response(JSON.stringify(obj), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json" },
  });
}