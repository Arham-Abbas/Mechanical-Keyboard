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
            Title = "Mechanical Keyboard Settings";

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);

            // Navigate to the initial page
            ContentFrame.Navigate(typeof(GeneralPage));
            NavView.SelectedItem = GeneralNavItem;
        }

        private void NavView_ItemInvoked(NavigationView _, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer is NavigationViewItem item)
            {
                Type? pageType = null;
                switch (item.Tag?.ToString())
                {
                    case "GeneralPage":
                        pageType = typeof(GeneralPage);
                        break;
                    case "SoundPacksPage":
                        pageType = typeof(SoundPacksPage);
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