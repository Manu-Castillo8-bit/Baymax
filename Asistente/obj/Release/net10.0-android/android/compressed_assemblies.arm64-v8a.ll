; ModuleID = 'compressed_assemblies.arm64-v8a.ll'
source_filename = "compressed_assemblies.arm64-v8a.ll"
target datalayout = "e-m:e-i8:8:32-i16:16:32-i64:64-i128:128-n32:64-S128"
target triple = "aarch64-unknown-linux-android21"

%struct.CompressedAssemblyDescriptor = type {
	i32, ; uint32_t uncompressed_file_size
	i1, ; bool loaded
	i32 ; uint32_t buffer_offset
}

@compressed_assembly_count = dso_local local_unnamed_addr constant i32 160, align 4

@compressed_assembly_descriptors = dso_local local_unnamed_addr global [160 x %struct.CompressedAssemblyDescriptor] [
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 0; uint32_t buffer_offset
	}, ; 0: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15624; uint32_t buffer_offset
	}, ; 1: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 31256; uint32_t buffer_offset
	}, ; 2: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 46880; uint32_t buffer_offset
	}, ; 3: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 62504; uint32_t buffer_offset
	}, ; 4: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 78136; uint32_t buffer_offset
	}, ; 5: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 93768; uint32_t buffer_offset
	}, ; 6: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 109400; uint32_t buffer_offset
	}, ; 7: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 125024; uint32_t buffer_offset
	}, ; 8: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 140648; uint32_t buffer_offset
	}, ; 9: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 156280; uint32_t buffer_offset
	}, ; 10: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 171904; uint32_t buffer_offset
	}, ; 11: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 187528; uint32_t buffer_offset
	}, ; 12: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 203152; uint32_t buffer_offset
	}, ; 13: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 218776; uint32_t buffer_offset
	}, ; 14: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 234400; uint32_t buffer_offset
	}, ; 15: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 250024; uint32_t buffer_offset
	}, ; 16: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 265648; uint32_t buffer_offset
	}, ; 17: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 281272; uint32_t buffer_offset
	}, ; 18: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15664, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 296904; uint32_t buffer_offset
	}, ; 19: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 312568; uint32_t buffer_offset
	}, ; 20: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 328192; uint32_t buffer_offset
	}, ; 21: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 343824; uint32_t buffer_offset
	}, ; 22: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 359456; uint32_t buffer_offset
	}, ; 23: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 375088; uint32_t buffer_offset
	}, ; 24: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 390760; uint32_t buffer_offset
	}, ; 25: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15664, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 406392; uint32_t buffer_offset
	}, ; 26: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 422056; uint32_t buffer_offset
	}, ; 27: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 437680; uint32_t buffer_offset
	}, ; 28: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 453304; uint32_t buffer_offset
	}, ; 29: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 468928; uint32_t buffer_offset
	}, ; 30: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15664, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 484552; uint32_t buffer_offset
	}, ; 31: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 500216; uint32_t buffer_offset
	}, ; 32: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 15632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 515840; uint32_t buffer_offset
	}, ; 33: Microsoft.Maui.Controls.resources
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 531472; uint32_t buffer_offset
	}, ; 34: _Microsoft.Android.Resource.Designer
	%struct.CompressedAssemblyDescriptor {
		i32 50688, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 537616; uint32_t buffer_offset
	}, ; 35: BirdMessenger
	%struct.CompressedAssemblyDescriptor {
		i32 14848, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 588304; uint32_t buffer_offset
	}, ; 36: Microsoft.Extensions.Configuration
	%struct.CompressedAssemblyDescriptor {
		i32 6656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 603152; uint32_t buffer_offset
	}, ; 37: Microsoft.Extensions.Configuration.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 46592, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 609808; uint32_t buffer_offset
	}, ; 38: Microsoft.Extensions.DependencyInjection
	%struct.CompressedAssemblyDescriptor {
		i32 26112, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 656400; uint32_t buffer_offset
	}, ; 39: Microsoft.Extensions.DependencyInjection.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 8192, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 682512; uint32_t buffer_offset
	}, ; 40: Microsoft.Extensions.Diagnostics.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 7168, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 690704; uint32_t buffer_offset
	}, ; 41: Microsoft.Extensions.FileProviders.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 697872; uint32_t buffer_offset
	}, ; 42: Microsoft.Extensions.Hosting.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 704016; uint32_t buffer_offset
	}, ; 43: Microsoft.Extensions.Logging
	%struct.CompressedAssemblyDescriptor {
		i32 19968, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 721936; uint32_t buffer_offset
	}, ; 44: Microsoft.Extensions.Logging.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 16896, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 741904; uint32_t buffer_offset
	}, ; 45: Microsoft.Extensions.Options
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 758800; uint32_t buffer_offset
	}, ; 46: Microsoft.Extensions.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 768016; uint32_t buffer_offset
	}, ; 47: Microsoft.IdentityModel.Abstractions
	%struct.CompressedAssemblyDescriptor {
		i32 132016, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 774160; uint32_t buffer_offset
	}, ; 48: Microsoft.IdentityModel.JsonWebTokens
	%struct.CompressedAssemblyDescriptor {
		i32 15872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 906176; uint32_t buffer_offset
	}, ; 49: Microsoft.IdentityModel.Logging
	%struct.CompressedAssemblyDescriptor {
		i32 291360, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 922048; uint32_t buffer_offset
	}, ; 50: Microsoft.IdentityModel.Tokens
	%struct.CompressedAssemblyDescriptor {
		i32 65968, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 1213408; uint32_t buffer_offset
	}, ; 51: Microsoft.IO.RecyclableMemoryStream
	%struct.CompressedAssemblyDescriptor {
		i32 1928504, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 1279376; uint32_t buffer_offset
	}, ; 52: Microsoft.Maui.Controls
	%struct.CompressedAssemblyDescriptor {
		i32 135432, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 3207880; uint32_t buffer_offset
	}, ; 53: Microsoft.Maui.Controls.Xaml
	%struct.CompressedAssemblyDescriptor {
		i32 861696, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 3343312; uint32_t buffer_offset
	}, ; 54: Microsoft.Maui
	%struct.CompressedAssemblyDescriptor {
		i32 67072, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4205008; uint32_t buffer_offset
	}, ; 55: Microsoft.Maui.Essentials
	%struct.CompressedAssemblyDescriptor {
		i32 208696, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4272080; uint32_t buffer_offset
	}, ; 56: Microsoft.Maui.Graphics
	%struct.CompressedAssemblyDescriptor {
		i32 300032, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4480776; uint32_t buffer_offset
	}, ; 57: MimeMapping
	%struct.CompressedAssemblyDescriptor {
		i32 153088, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4780808; uint32_t buffer_offset
	}, ; 58: Plugin.LocalNotification
	%struct.CompressedAssemblyDescriptor {
		i32 35328, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4933896; uint32_t buffer_offset
	}, ; 59: Plugin.LocalNotification.Core
	%struct.CompressedAssemblyDescriptor {
		i32 79872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 4969224; uint32_t buffer_offset
	}, ; 60: SQLite-net
	%struct.CompressedAssemblyDescriptor {
		i32 51712, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5049096; uint32_t buffer_offset
	}, ; 61: SQLitePCLRaw.core
	%struct.CompressedAssemblyDescriptor {
		i32 36864, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5100808; uint32_t buffer_offset
	}, ; 62: SQLitePCLRaw.provider.e_sqlite3
	%struct.CompressedAssemblyDescriptor {
		i32 24576, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5137672; uint32_t buffer_offset
	}, ; 63: Supabase
	%struct.CompressedAssemblyDescriptor {
		i32 28672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5162248; uint32_t buffer_offset
	}, ; 64: Supabase.Core
	%struct.CompressedAssemblyDescriptor {
		i32 31232, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5190920; uint32_t buffer_offset
	}, ; 65: Supabase.Functions
	%struct.CompressedAssemblyDescriptor {
		i32 158208, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5222152; uint32_t buffer_offset
	}, ; 66: Supabase.Gotrue
	%struct.CompressedAssemblyDescriptor {
		i32 92160, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5380360; uint32_t buffer_offset
	}, ; 67: Supabase.Postgrest
	%struct.CompressedAssemblyDescriptor {
		i32 83456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5472520; uint32_t buffer_offset
	}, ; 68: Supabase.Realtime
	%struct.CompressedAssemblyDescriptor {
		i32 83456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5555976; uint32_t buffer_offset
	}, ; 69: Supabase.Storage
	%struct.CompressedAssemblyDescriptor {
		i32 83904, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5639432; uint32_t buffer_offset
	}, ; 70: System.IdentityModel.Tokens.Jwt
	%struct.CompressedAssemblyDescriptor {
		i32 43520, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5723336; uint32_t buffer_offset
	}, ; 71: System.Reactive
	%struct.CompressedAssemblyDescriptor {
		i32 54272, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5766856; uint32_t buffer_offset
	}, ; 72: Websocket.Client
	%struct.CompressedAssemblyDescriptor {
		i32 73728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5821128; uint32_t buffer_offset
	}, ; 73: Xamarin.AndroidX.Activity
	%struct.CompressedAssemblyDescriptor {
		i32 583680, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 5894856; uint32_t buffer_offset
	}, ; 74: Xamarin.AndroidX.AppCompat
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6478536; uint32_t buffer_offset
	}, ; 75: Xamarin.AndroidX.AppCompat.AppCompatResources
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6496456; uint32_t buffer_offset
	}, ; 76: Xamarin.AndroidX.CardView
	%struct.CompressedAssemblyDescriptor {
		i32 22528, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6515400; uint32_t buffer_offset
	}, ; 77: Xamarin.AndroidX.Collection.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 78336, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6537928; uint32_t buffer_offset
	}, ; 78: Xamarin.AndroidX.CoordinatorLayout
	%struct.CompressedAssemblyDescriptor {
		i32 827392, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 6616264; uint32_t buffer_offset
	}, ; 79: Xamarin.AndroidX.Core
	%struct.CompressedAssemblyDescriptor {
		i32 26624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7443656; uint32_t buffer_offset
	}, ; 80: Xamarin.AndroidX.CursorAdapter
	%struct.CompressedAssemblyDescriptor {
		i32 9728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7470280; uint32_t buffer_offset
	}, ; 81: Xamarin.AndroidX.CustomView
	%struct.CompressedAssemblyDescriptor {
		i32 47104, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7480008; uint32_t buffer_offset
	}, ; 82: Xamarin.AndroidX.DrawerLayout
	%struct.CompressedAssemblyDescriptor {
		i32 236032, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7527112; uint32_t buffer_offset
	}, ; 83: Xamarin.AndroidX.Fragment
	%struct.CompressedAssemblyDescriptor {
		i32 23552, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7763144; uint32_t buffer_offset
	}, ; 84: Xamarin.AndroidX.Lifecycle.Common.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 18944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7786696; uint32_t buffer_offset
	}, ; 85: Xamarin.AndroidX.Lifecycle.LiveData.Core
	%struct.CompressedAssemblyDescriptor {
		i32 32768, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7805640; uint32_t buffer_offset
	}, ; 86: Xamarin.AndroidX.Lifecycle.ViewModel.Android
	%struct.CompressedAssemblyDescriptor {
		i32 13824, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7838408; uint32_t buffer_offset
	}, ; 87: Xamarin.AndroidX.Lifecycle.ViewModelSavedState.Android
	%struct.CompressedAssemblyDescriptor {
		i32 39424, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7852232; uint32_t buffer_offset
	}, ; 88: Xamarin.AndroidX.Loader
	%struct.CompressedAssemblyDescriptor {
		i32 92672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7891656; uint32_t buffer_offset
	}, ; 89: Xamarin.AndroidX.Navigation.Common.Android
	%struct.CompressedAssemblyDescriptor {
		i32 19456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 7984328; uint32_t buffer_offset
	}, ; 90: Xamarin.AndroidX.Navigation.Fragment
	%struct.CompressedAssemblyDescriptor {
		i32 65536, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8003784; uint32_t buffer_offset
	}, ; 91: Xamarin.AndroidX.Navigation.Runtime.Android
	%struct.CompressedAssemblyDescriptor {
		i32 27136, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8069320; uint32_t buffer_offset
	}, ; 92: Xamarin.AndroidX.Navigation.UI
	%struct.CompressedAssemblyDescriptor {
		i32 457728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8096456; uint32_t buffer_offset
	}, ; 93: Xamarin.AndroidX.RecyclerView
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8554184; uint32_t buffer_offset
	}, ; 94: Xamarin.AndroidX.SavedState.SavedState.Android
	%struct.CompressedAssemblyDescriptor {
		i32 41984, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8566472; uint32_t buffer_offset
	}, ; 95: Xamarin.AndroidX.SwipeRefreshLayout
	%struct.CompressedAssemblyDescriptor {
		i32 9728, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8608456; uint32_t buffer_offset
	}, ; 96: Xamarin.AndroidX.VersionedParcelable
	%struct.CompressedAssemblyDescriptor {
		i32 62976, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8618184; uint32_t buffer_offset
	}, ; 97: Xamarin.AndroidX.ViewPager
	%struct.CompressedAssemblyDescriptor {
		i32 40448, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8681160; uint32_t buffer_offset
	}, ; 98: Xamarin.AndroidX.ViewPager2
	%struct.CompressedAssemblyDescriptor {
		i32 675840, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 8721608; uint32_t buffer_offset
	}, ; 99: Xamarin.Google.Android.Material
	%struct.CompressedAssemblyDescriptor {
		i32 90624, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9397448; uint32_t buffer_offset
	}, ; 100: Xamarin.Kotlin.StdLib
	%struct.CompressedAssemblyDescriptor {
		i32 28672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9488072; uint32_t buffer_offset
	}, ; 101: Xamarin.KotlinX.Coroutines.Core.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 91648, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9516744; uint32_t buffer_offset
	}, ; 102: Xamarin.KotlinX.Serialization.Core.Jvm
	%struct.CompressedAssemblyDescriptor {
		i32 326656, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9608392; uint32_t buffer_offset
	}, ; 103: Asistente
	%struct.CompressedAssemblyDescriptor {
		i32 28672, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9935048; uint32_t buffer_offset
	}, ; 104: System.Collections.Concurrent
	%struct.CompressedAssemblyDescriptor {
		i32 17408, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9963720; uint32_t buffer_offset
	}, ; 105: System.Collections.NonGeneric
	%struct.CompressedAssemblyDescriptor {
		i32 15872, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9981128; uint32_t buffer_offset
	}, ; 106: System.Collections.Specialized
	%struct.CompressedAssemblyDescriptor {
		i32 35840, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 9997000; uint32_t buffer_offset
	}, ; 107: System.Collections
	%struct.CompressedAssemblyDescriptor {
		i32 11776, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10032840; uint32_t buffer_offset
	}, ; 108: System.ComponentModel.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 94720, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10044616; uint32_t buffer_offset
	}, ; 109: System.ComponentModel.TypeConverter
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10139336; uint32_t buffer_offset
	}, ; 110: System.ComponentModel
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10144456; uint32_t buffer_offset
	}, ; 111: System.Console
	%struct.CompressedAssemblyDescriptor {
		i32 51712, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10156744; uint32_t buffer_offset
	}, ; 112: System.Diagnostics.DiagnosticSource
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10208456; uint32_t buffer_offset
	}, ; 113: System.Diagnostics.TraceSource
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10226376; uint32_t buffer_offset
	}, ; 114: System.Diagnostics.Tracing
	%struct.CompressedAssemblyDescriptor {
		i32 11776, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10231496; uint32_t buffer_offset
	}, ; 115: System.Drawing.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10243272; uint32_t buffer_offset
	}, ; 116: System.Drawing
	%struct.CompressedAssemblyDescriptor {
		i32 62464, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10248392; uint32_t buffer_offset
	}, ; 117: System.Formats.Asn1
	%struct.CompressedAssemblyDescriptor {
		i32 22016, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10310856; uint32_t buffer_offset
	}, ; 118: System.IO.Compression.Brotli
	%struct.CompressedAssemblyDescriptor {
		i32 32768, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10332872; uint32_t buffer_offset
	}, ; 119: System.IO.Compression
	%struct.CompressedAssemblyDescriptor {
		i32 6144, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10365640; uint32_t buffer_offset
	}, ; 120: System.IO.Pipelines
	%struct.CompressedAssemblyDescriptor {
		i32 362496, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10371784; uint32_t buffer_offset
	}, ; 121: System.Linq.Expressions
	%struct.CompressedAssemblyDescriptor {
		i32 69120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10734280; uint32_t buffer_offset
	}, ; 122: System.Linq
	%struct.CompressedAssemblyDescriptor {
		i32 18432, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10803400; uint32_t buffer_offset
	}, ; 123: System.Memory
	%struct.CompressedAssemblyDescriptor {
		i32 365568, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 10821832; uint32_t buffer_offset
	}, ; 124: System.Net.Http
	%struct.CompressedAssemblyDescriptor {
		i32 27648, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11187400; uint32_t buffer_offset
	}, ; 125: System.Net.NameResolution
	%struct.CompressedAssemblyDescriptor {
		i32 25600, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11215048; uint32_t buffer_offset
	}, ; 126: System.Net.NetworkInformation
	%struct.CompressedAssemblyDescriptor {
		i32 8192, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11240648; uint32_t buffer_offset
	}, ; 127: System.Net.Ping
	%struct.CompressedAssemblyDescriptor {
		i32 68608, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11248840; uint32_t buffer_offset
	}, ; 128: System.Net.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 7680, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11317448; uint32_t buffer_offset
	}, ; 129: System.Net.Requests
	%struct.CompressedAssemblyDescriptor {
		i32 146944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11325128; uint32_t buffer_offset
	}, ; 130: System.Net.Security
	%struct.CompressedAssemblyDescriptor {
		i32 101376, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11472072; uint32_t buffer_offset
	}, ; 131: System.Net.Sockets
	%struct.CompressedAssemblyDescriptor {
		i32 14848, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11573448; uint32_t buffer_offset
	}, ; 132: System.Net.WebHeaderCollection
	%struct.CompressedAssemblyDescriptor {
		i32 27136, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11588296; uint32_t buffer_offset
	}, ; 133: System.Net.WebSockets.Client
	%struct.CompressedAssemblyDescriptor {
		i32 61440, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11615432; uint32_t buffer_offset
	}, ; 134: System.Net.WebSockets
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11676872; uint32_t buffer_offset
	}, ; 135: System.Numerics.Vectors
	%struct.CompressedAssemblyDescriptor {
		i32 17920, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11681992; uint32_t buffer_offset
	}, ; 136: System.ObjectModel
	%struct.CompressedAssemblyDescriptor {
		i32 75264, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11699912; uint32_t buffer_offset
	}, ; 137: System.Private.Uri
	%struct.CompressedAssemblyDescriptor {
		i32 397312, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 11775176; uint32_t buffer_offset
	}, ; 138: System.Private.Xml
	%struct.CompressedAssemblyDescriptor {
		i32 9216, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12172488; uint32_t buffer_offset
	}, ; 139: System.Runtime.InteropServices
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12181704; uint32_t buffer_offset
	}, ; 140: System.Runtime.Loader
	%struct.CompressedAssemblyDescriptor {
		i32 83456, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12186824; uint32_t buffer_offset
	}, ; 141: System.Runtime.Numerics
	%struct.CompressedAssemblyDescriptor {
		i32 5632, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12270280; uint32_t buffer_offset
	}, ; 142: System.Runtime.Serialization.Primitives
	%struct.CompressedAssemblyDescriptor {
		i32 15360, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12275912; uint32_t buffer_offset
	}, ; 143: System.Runtime
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12291272; uint32_t buffer_offset
	}, ; 144: System.Security.Claims
	%struct.CompressedAssemblyDescriptor {
		i32 274944, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12303560; uint32_t buffer_offset
	}, ; 145: System.Security.Cryptography
	%struct.CompressedAssemblyDescriptor {
		i32 29696, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12578504; uint32_t buffer_offset
	}, ; 146: System.Text.Encodings.Web
	%struct.CompressedAssemblyDescriptor {
		i32 398848, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 12608200; uint32_t buffer_offset
	}, ; 147: System.Text.Json
	%struct.CompressedAssemblyDescriptor {
		i32 333824, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13007048; uint32_t buffer_offset
	}, ; 148: System.Text.RegularExpressions
	%struct.CompressedAssemblyDescriptor {
		i32 24064, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13340872; uint32_t buffer_offset
	}, ; 149: System.Threading.Channels
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13364936; uint32_t buffer_offset
	}, ; 150: System.Threading.Thread
	%struct.CompressedAssemblyDescriptor {
		i32 12288, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13370056; uint32_t buffer_offset
	}, ; 151: System.Threading
	%struct.CompressedAssemblyDescriptor {
		i32 10752, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13382344; uint32_t buffer_offset
	}, ; 152: System.Web.HttpUtility
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13393096; uint32_t buffer_offset
	}, ; 153: System.Xml.ReaderWriter
	%struct.CompressedAssemblyDescriptor {
		i32 5120, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13398216; uint32_t buffer_offset
	}, ; 154: System
	%struct.CompressedAssemblyDescriptor {
		i32 12800, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13403336; uint32_t buffer_offset
	}, ; 155: netstandard
	%struct.CompressedAssemblyDescriptor {
		i32 2081792, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 13416136; uint32_t buffer_offset
	}, ; 156: System.Private.CoreLib
	%struct.CompressedAssemblyDescriptor {
		i32 171008, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15497928; uint32_t buffer_offset
	}, ; 157: Java.Interop
	%struct.CompressedAssemblyDescriptor {
		i32 22368, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15668936; uint32_t buffer_offset
	}, ; 158: Mono.Android.Runtime
	%struct.CompressedAssemblyDescriptor {
		i32 2092544, ; uint32_t uncompressed_file_size
		i1 false, ; bool loaded
		i32 15691304; uint32_t buffer_offset
	} ; 159: Mono.Android
], align 4

@uncompressed_assemblies_data_size = dso_local local_unnamed_addr constant i32 17783848, align 4

@uncompressed_assemblies_data_buffer = dso_local local_unnamed_addr global [17783848 x i8] zeroinitializer, align 1

; Metadata
!llvm.module.flags = !{!0, !1, !7, !8, !9, !10}
!0 = !{i32 1, !"wchar_size", i32 4}
!1 = !{i32 7, !"PIC Level", i32 2}
!llvm.ident = !{!2}
!2 = !{!".NET for Android remotes/origin/release/10.0.1xx @ d549e1dc4e2a083b08b4f24cb5495e81b99d79b5"}
!3 = !{!4, !4, i64 0}
!4 = !{!"any pointer", !5, i64 0}
!5 = !{!"omnipotent char", !6, i64 0}
!6 = !{!"Simple C++ TBAA"}
!7 = !{i32 1, !"branch-target-enforcement", i32 0}
!8 = !{i32 1, !"sign-return-address", i32 0}
!9 = !{i32 1, !"sign-return-address-all", i32 0}
!10 = !{i32 1, !"sign-return-address-with-bkey", i32 0}
