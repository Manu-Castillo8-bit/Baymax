package crc64e53d2f592022988e;


public class ConnectivityImplementation_EssentialsNetworkCallback
	extends android.net.ConnectivityManager.NetworkCallback
	implements
		mono.android.IGCUserPeer
{

	public ConnectivityImplementation_EssentialsNetworkCallback ()
	{
		super ();
		if (getClass () == ConnectivityImplementation_EssentialsNetworkCallback.class) {
			mono.android.TypeManager.Activate ("Microsoft.Maui.Networking.ConnectivityImplementation+EssentialsNetworkCallback, Microsoft.Maui.Essentials", "", this, new java.lang.Object[] {  });
		}
	}

	public void onAvailable (android.net.Network p0)
	{
		n_onAvailable (p0);
	}

	private native void n_onAvailable (android.net.Network p0);

	public void onLost (android.net.Network p0)
	{
		n_onLost (p0);
	}

	private native void n_onLost (android.net.Network p0);

	public void onCapabilitiesChanged (android.net.Network p0, android.net.NetworkCapabilities p1)
	{
		n_onCapabilitiesChanged (p0, p1);
	}

	private native void n_onCapabilitiesChanged (android.net.Network p0, android.net.NetworkCapabilities p1);

	public void onUnavailable ()
	{
		n_onUnavailable ();
	}

	private native void n_onUnavailable ();

	public void onLinkPropertiesChanged (android.net.Network p0, android.net.LinkProperties p1)
	{
		n_onLinkPropertiesChanged (p0, p1);
	}

	private native void n_onLinkPropertiesChanged (android.net.Network p0, android.net.LinkProperties p1);

	public void onLosing (android.net.Network p0, int p1)
	{
		n_onLosing (p0, p1);
	}

	private native void n_onLosing (android.net.Network p0, int p1);

	private java.util.ArrayList refList;
	public void monodroidAddReference (java.lang.Object obj)
	{
		if (refList == null)
			refList = new java.util.ArrayList ();
		refList.add (obj);
	}

	public void monodroidClearReferences ()
	{
		if (refList != null)
			refList.clear ();
	}
}
