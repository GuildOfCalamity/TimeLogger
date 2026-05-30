using System.Windows;

namespace TimeLogger;

public partial class EditEntryWindow : Window
{
    public EditEntryWindow()
    {
        InitializeComponent();
        //SourceInitialized += (s, e) => DarkTitleBar.Apply(this); // if not using XAML approach
    }
}
