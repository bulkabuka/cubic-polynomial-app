// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Runtime.InteropServices; // For DllImport
using WinRT; // required to support Window.As<ICompositionSupportsSystemBackdrop>()
using Microsoft.UI.Composition.SystemBackdrops;
using System.Xml.Linq;
using Microsoft.UI;
using System.ComponentModel;
using Windows.ApplicationModel.Core;
using System.Drawing;
using Point = System.Drawing.Point;
using Microsoft.UI.Xaml.Shapes;
using Windows.Graphics.Capture;


// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace CubicPolynomialApp
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private WindowsSystemDispatcherQueueHelper m_wsdqHelper; // See separate sample below for implementation
        Microsoft.UI.Composition.SystemBackdrops.MicaController m_micaController;
        Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration m_configurationSource;

        public MainWindow()
        {
            this.InitializeComponent();
            TrySetMicaBackdrop();
        }

        bool TrySetMicaBackdrop()
        {
            if (Microsoft.UI.Composition.SystemBackdrops.MicaController.IsSupported())
            {
                m_wsdqHelper = new WindowsSystemDispatcherQueueHelper();
                m_wsdqHelper.EnsureWindowsSystemDispatcherQueueController();

                // Hooking up the policy object
                m_configurationSource = new Microsoft.UI.Composition.SystemBackdrops.SystemBackdropConfiguration();
                this.Activated += Window_Activated;
                this.Closed += Window_Closed;
                ((FrameworkElement)this.Content).ActualThemeChanged += Window_ThemeChanged;

                // Initial configuration state.
                m_configurationSource.IsInputActive = true;
                SetConfigurationSourceTheme();

                m_micaController = new Microsoft.UI.Composition.SystemBackdrops.MicaController();

                // Enable the system backdrop.
                // Note: Be sure to have "using WinRT;" to support the Window.As<...>() call.
                m_micaController.AddSystemBackdropTarget(this.As<Microsoft.UI.Composition.ICompositionSupportsSystemBackdrop>());
                m_micaController.SetSystemBackdropConfiguration(m_configurationSource);
                return true; // succeeded
            }

            return false; // Mica is not supported on this system
        }

        private void Window_Activated(object sender, WindowActivatedEventArgs args)
        {
            m_configurationSource.IsInputActive = args.WindowActivationState != WindowActivationState.Deactivated;
        }

        private void Window_Closed(object sender, WindowEventArgs args)
        {
            // Make sure any Mica/Acrylic controller is disposed so it doesn't try to
            // use this closed window.
            if (m_micaController != null)
            {
                m_micaController.Dispose();
                m_micaController = null;
            }
            this.Activated -= Window_Activated;
            m_configurationSource = null;
        }

        private void Window_ThemeChanged(FrameworkElement sender, object args)
        {
            if (m_configurationSource != null)
            {
                SetConfigurationSourceTheme();
            }
        }

        private void SetConfigurationSourceTheme()
        {
            switch (((FrameworkElement)this.Content).ActualTheme)
            {
                case ElementTheme.Dark: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Dark; break;
                case ElementTheme.Light: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Light; break;
                case ElementTheme.Default: m_configurationSource.Theme = Microsoft.UI.Composition.SystemBackdrops.SystemBackdropTheme.Default; break;
            }
        }

        internal class WindowsSystemDispatcherQueueHelper
        {
            [StructLayout(LayoutKind.Sequential)]
            private struct DispatcherQueueOptions
            {
                internal int dwSize;
                internal int threadType;
                internal int apartmentType;
            }

            [DllImport("CoreMessaging.dll")]
            private static extern int CreateDispatcherQueueController([In] DispatcherQueueOptions options, [In, Out, MarshalAs(UnmanagedType.IUnknown)] ref object dispatcherQueueController);

            private object _mDispatcherQueueController;
            public void EnsureWindowsSystemDispatcherQueueController()
            {
                if (Windows.System.DispatcherQueue.GetForCurrentThread() != null)
                {
                    // one already exists, so we'll just use it.
                    return;
                }

                if (_mDispatcherQueueController != null) return;
                DispatcherQueueOptions options;
                options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
                options.threadType = 2;    // DQTYPE_THREAD_CURRENT
                options.apartmentType = 2; // DQTAT_COM_STA

                CreateDispatcherQueueController(options, ref _mDispatcherQueueController);
            }
            
            
        }

        private void SolveBtn_OnClick(object sender, RoutedEventArgs e)
        {
            var a = double.Parse(AFactor.Text);
            var b = double.Parse(BFactor.Text);
            var c = double.Parse(CFactor.Text);
            var d = double.Parse(DFactor.Text);
            var x1 = double.Parse(StartX.Text);
            var x2 = double.Parse(EndX.Text);
            DataList.Items.Clear();
            // получаем все значения с полей для ввода и очищаем предыдущие решения

            var f1 = a * x1 * x1 * x1 + b * x1 * x1 + c * x1 + d;
            var f2 = a * x2 * x2 * x2 + b * x2 * x2 + c * x2 + d;
            // считаем значения на концах промежутка
            var i = 1;
            while (Math.Abs(x1 - x2) > 0.001) // работаем до тех пор пока разница между шагами не станет меньше точности
            {
                var x3 = (x1 + x2) / 2; // получаем методом половинного деления новую часть промежутка (уточняем его)
                var f3 = a * x3 * x3 * x3 + b * x3 * x3 + c * x3 + d; // находим в этой точке значение функции
                DataList.Items.Add($"Шаг №{i}, Начальный промежуток: [{x1}, {x2}]");
                if (f1 * f3 < 0)
                {
                    x2 = x3;
                    f2 = f3;
                }
                else
                {
                    x1 = x3;
                    f1 = f3;
                }
                DataList.Items.Add($"Точка: {x3}, Функция в этой точке: {f3}, Итоговый промежуток: [{x1}, {x2}]");
                i++;
            }
            
            DataList.Items.Add("Корень уравнения: " + (x1+x2) / 2);
        }
    }
}
