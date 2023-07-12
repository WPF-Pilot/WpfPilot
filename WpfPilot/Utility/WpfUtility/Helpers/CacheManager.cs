namespace WpfPilot.Utility.WpfUtility.Helpers;

using System;

internal interface ICacheManaged : IDisposable
{
	void Activate();
}