namespace WpfPilot.Utility.WpfUtility.Diagnostics.Providers;

using WpfPilot.Utility.WpfUtility.Diagnostics;

internal class MissingAdornerLayerDiagnosticProvider : DiagnosticProvider
{
	public static readonly MissingAdornerLayerDiagnosticProvider Instance = new();

	public override string Name => "Missing adorner layer";

	public override string Description => "No adorner layer for the selected element could be found";
}