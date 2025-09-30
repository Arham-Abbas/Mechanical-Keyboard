using Mechanical_Keyboard.Views;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

namespace Mechanical_Keyboard
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Title = "Mechanical Keyboard";

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Navigate to the single SettingsPage by default
            ContentFrame.Navigate(typeof(SettingsPage));
            NavView.SelectedItem = SettingsNavItem;
            NavView.Header = "Settings";
        }

        private void NavView_ItemInvoked(NavigationView _, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                NavView.Header = item.Content.ToString();

                Type? pageType = null;
                switch (item.Tag?.ToString())
                {
                    case "SettingsPage":
                        pageType = typeof(SettingsPage);
                        break;
                    case "AboutPage":
                        pageType = typeof(AboutPage);
                        break;
                }

                if (pageType != null && ContentFrame.CurrentSourcePageType != pageType)
                {
                    ContentFrame.Navigate(pageType);
                }
            }
        }
    }
}