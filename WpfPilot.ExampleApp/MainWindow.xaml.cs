namespace WpfPilot.ExampleApp;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

public partial class MainWindow : Window
{
	public MainWindow()
	{
		InitializeComponent();

		RoutedCommand ctrlA = new();
		ctrlA.InputGestures.Add(new KeyGesture(Key.A, ModifierKeys.Control));
		CommandBindings.Add(new CommandBinding(ctrlA, CtrlA_Shortcut));
	}

	private void HelloWorldButton_Click(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "HelloWorldButton_Click event triggered.";
	}

	private void HelloWorldButton_DoubleClick(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "HelloWorldButton_DoubleClick event triggered.";
	}

	private void HelloWorldButton_RightClick(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "HelloWorldButton_RightClick event triggered.";
	}

	private void HelloWorldButton_ToolTipOpening(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "HelloWorldButton_ToolTipOpening event triggered.";
	}

	private void MainCheckbox_Checked(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "MainCheckbox_Checked event triggered.";
	}

	private void MainCheckbox_Unchecked(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "MainCheckbox_Unchecked event triggered.";
	}

	private void TextBox1_TextChanged(object sender, TextChangedEventArgs e)
	{
		EventDisplay.Text = "TextBox1_TextChanged event triggered.";
	}

	private void TextBox1_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
	{
		EventDisplay.Text = "TextBox1_GotKeyboardFocus event triggered.";
	}

	private void TextBox1_TouchDown(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "TextBox1_TouchDown event triggered.";
	}

	private void TextBox1_SelectionChanged(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "TextBox1_SelectionChanged event triggered.";
	}

	private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs args)
	{
		var listBox = (ListBox) sender;
		var listBoxItem = (ListBoxItem) listBox.SelectedItem;
		EventDisplay.Text = $"{listBoxItem.Name} selected event triggered.";
	}

	private void ExpanderControl_Expanded(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "ExpanderControl_Expanded event triggered.";
	}

	private void ExpanderControl_Collapsed(object sender, RoutedEventArgs e)
	{
		EventDisplay.Text = "ExpanderControl_Collapsed event triggered.";
	}

	private void CtrlA_Shortcut(object sender, ExecutedRoutedEventArgs e)
	{
		EventDisplay.Text = "Ctrl+A shortcut triggered.";
	}

	private void OpenOtherWindow_Click(object sender, RoutedEventArgs e)
	{
		OtherWindow otherWindow = new OtherWindow();
		otherWindow.Show();
	}
}
