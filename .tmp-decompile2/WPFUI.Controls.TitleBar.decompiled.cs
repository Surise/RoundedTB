using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using WPFUI.Common;
using WPFUI.Tray;

namespace WPFUI.Controls;

/// <summary>
/// Custom navigation buttons for the window.
/// </summary>
public class TitleBar : UserControl
{
	private Window _parent;

	private NotifyIcon _notifyIcon;

	private SnapLayout _snapLayout;

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.Title" />.
	/// </summary>
	public static readonly DependencyProperty TitleProperty = DependencyProperty.Register("Title", typeof(string), typeof(TitleBar), new PropertyMetadata((PropertyChangedCallback)null));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.MinimizeToTray" />.
	/// </summary>
	public static readonly DependencyProperty MinimizeToTrayProperty = DependencyProperty.Register("MinimizeToTray", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)false));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.UseSnapLayout" />.
	/// </summary>
	public static readonly DependencyProperty UseSnapLayoutProperty = DependencyProperty.Register("UseSnapLayout", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)false));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.IsMaximized" />.
	/// </summary>
	public static readonly DependencyProperty IsMaximizedProperty = DependencyProperty.Register("IsMaximized", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)false));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.ApplicationNavigation" />.
	/// </summary>
	public static readonly DependencyProperty ApplicationNavigationProperty = DependencyProperty.Register("ApplicationNavigation", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)false));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.ShowMaximize" />.
	/// </summary>
	public static readonly DependencyProperty ShowMaximizeProperty = DependencyProperty.Register("ShowMaximize", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)true));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.ShowMinimize" />.
	/// </summary>
	public static readonly DependencyProperty ShowMinimizeProperty = DependencyProperty.Register("ShowMinimize", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)true));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.Icon" />.
	/// </summary>
	public static readonly DependencyProperty IconProperty = DependencyProperty.Register("Icon", typeof(ImageSource), typeof(TitleBar), new PropertyMetadata((PropertyChangedCallback)null));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.NotifyIconTooltip" />.
	/// </summary>
	public static readonly DependencyProperty NotifyIconTooltipProperty = DependencyProperty.Register("NotifyIconTooltip", typeof(string), typeof(TitleBar), new PropertyMetadata((object)string.Empty, new PropertyChangedCallback(NotifyIconTooltip_OnChanged)));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.NotifyIconImage" />.
	/// </summary>
	public static readonly DependencyProperty NotifyIconImageProperty = DependencyProperty.Register("NotifyIconImage", typeof(ImageSource), typeof(TitleBar), new PropertyMetadata((PropertyChangedCallback)null));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.UseNotifyIcon" />.
	/// </summary>
	public static readonly DependencyProperty UseNotifyIconProperty = DependencyProperty.Register("UseNotifyIcon", typeof(bool), typeof(TitleBar), new PropertyMetadata((object)false, new PropertyChangedCallback(UseNotifyIcon_OnChanged)));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.NotifyIconMenu" />.
	/// </summary>
	public static readonly DependencyProperty NotifyIconMenuProperty = DependencyProperty.Register("NotifyIconMenu", typeof(ContextMenu), typeof(TitleBar), new PropertyMetadata((object)null, new PropertyChangedCallback(NotifyIconMenu_OnChanged)));

	/// <summary>
	/// Routed event for <see cref="E:WPFUI.Controls.TitleBar.NotifyIconClick" />.
	/// </summary>
	public static readonly RoutedEvent NotifyIconClickEvent = EventManager.RegisterRoutedEvent("NotifyIconClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TitleBar));

	/// <summary>
	/// Routed event for <see cref="E:WPFUI.Controls.TitleBar.NotifyIconDoubleClick" />.
	/// </summary>
	public static readonly RoutedEvent NotifyIconDoubleClickEvent = EventManager.RegisterRoutedEvent("NotifyIconDoubleClick", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TitleBar));

	/// <summary>
	/// Routed event for <see cref="E:WPFUI.Controls.TitleBar.CloseClicked" />.
	/// </summary>
	public static readonly RoutedEvent CloseClickedEvent = EventManager.RegisterRoutedEvent("CloseClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TitleBar));

	/// <summary>
	/// Routed event for <see cref="E:WPFUI.Controls.TitleBar.MaximizeClicked" />.
	/// </summary>
	public static readonly RoutedEvent MaximizeClickedEvent = EventManager.RegisterRoutedEvent("MaximizeClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TitleBar));

	/// <summary>
	/// Routed event for <see cref="E:WPFUI.Controls.TitleBar.MinimizeClicked" />.
	/// </summary>
	public static readonly RoutedEvent MinimizeClickedEvent = EventManager.RegisterRoutedEvent("MinimizeClicked", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TitleBar));

	/// <summary>
	/// Property for <see cref="P:WPFUI.Controls.TitleBar.ButtonCommand" />.
	/// </summary>
	public static readonly DependencyProperty ButtonCommandProperty = DependencyProperty.Register("ButtonCommand", typeof(RelayCommand), typeof(TitleBar), new PropertyMetadata((PropertyChangedCallback)null));

	/// <summary>
	/// Gets or sets title displayed on the left.
	/// </summary>
	public string Title
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(TitleProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(TitleProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether to minimize the application to tray.
	/// </summary>
	public bool MinimizeToTray
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(MinimizeToTrayProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(MinimizeToTrayProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether the use Windows 11 Snap Layout.
	/// </summary>
	public bool UseSnapLayout
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(UseSnapLayoutProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(UseSnapLayoutProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether the current window is maximized.
	/// </summary>
	public bool IsMaximized
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(IsMaximizedProperty);
		}
		internal set
		{
			((DependencyObject)this).SetValue(IsMaximizedProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether the controls affect main application window.
	/// </summary>
	public bool ApplicationNavigation
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(ApplicationNavigationProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ApplicationNavigationProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether to show maximize button.
	/// </summary>
	public bool ShowMaximize
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(ShowMaximizeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ShowMaximizeProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether to show minimize button.
	/// </summary>
	public bool ShowMinimize
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(ShowMinimizeProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(ShowMinimizeProperty, (object)value);
		}
	}

	/// <summary>
	/// Titlebar icon.
	/// </summary>
	public ImageSource Icon
	{
		get
		{
			return (ImageSource)((DependencyObject)this).GetValue(IconProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(IconProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets text displayed when hover NotifyIcon in system tray.
	/// </summary>
	public string NotifyIconTooltip
	{
		get
		{
			return (string)((DependencyObject)this).GetValue(NotifyIconTooltipProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(NotifyIconTooltipProperty, (object)value);
		}
	}

	/// <summary>
	/// BitmapSource of tray icon.
	/// </summary>
	public ImageSource NotifyIconImage
	{
		get
		{
			return (ImageSource)((DependencyObject)this).GetValue(NotifyIconImageProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(NotifyIconImageProperty, (object)value);
		}
	}

	/// <summary>
	/// Gets or sets information whether to use shell icon with menu in system tray.
	/// </summary>
	public bool UseNotifyIcon
	{
		get
		{
			return (bool)((DependencyObject)this).GetValue(UseNotifyIconProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(UseNotifyIconProperty, (object)value);
		}
	}

	/// <summary>
	/// Menu displayed when left click on NotifyIcon.
	/// </summary>
	public ContextMenu NotifyIconMenu
	{
		get
		{
			return (ContextMenu)((DependencyObject)this).GetValue(NotifyIconMenuProperty);
		}
		set
		{
			((DependencyObject)this).SetValue(NotifyIconMenuProperty, (object)value);
		}
	}

	/// <summary>
	/// Command triggered after clicking the titlebar button.
	/// </summary>
	public RelayCommand ButtonCommand => (RelayCommand)((DependencyObject)this).GetValue(ButtonCommandProperty);

	/// <summary>
	/// Lets you override the behavior of the Close button with an <see cref="T:System.Action" />.
	/// </summary>
	public Action<TitleBar, Window> CloseActionOverride { get; set; }

	/// <summary>
	/// Lets you override the behavior of the Maximize/Restore button with an <see cref="T:System.Action" />.
	/// </summary>
	public Action<TitleBar, Window> MaximizeActionOverride { get; set; }

	/// <summary>
	/// Lets you override the behavior of the Minimize button with an <see cref="T:System.Action" />.
	/// </summary>
	public Action<TitleBar, Window> MinimizeActionOverride { get; set; }

	private Window ParentWindow
	{
		get
		{
			if (_parent == null)
			{
				_parent = Window.GetWindow((DependencyObject)(object)this);
			}
			return _parent;
		}
	}

	/// <summary>
	/// Event triggered after clicking the left mouse button on the tray icon.
	/// </summary>
	public event RoutedEventHandler NotifyIconClick
	{
		add
		{
			AddHandler(NotifyIconClickEvent, value);
		}
		remove
		{
			RemoveHandler(NotifyIconClickEvent, value);
		}
	}

	/// <summary>
	/// Event triggered after double-clicking the left mouse button on the tray icon.
	/// </summary>
	public event RoutedEventHandler NotifyIconDoubleClick
	{
		add
		{
			AddHandler(NotifyIconDoubleClickEvent, value);
		}
		remove
		{
			RemoveHandler(NotifyIconDoubleClickEvent, value);
		}
	}

	/// <summary>
	/// Event triggered after clicking close button.
	/// </summary>
	public event RoutedEventHandler CloseClicked
	{
		add
		{
			AddHandler(CloseClickedEvent, value);
		}
		remove
		{
			RemoveHandler(CloseClickedEvent, value);
		}
	}

	/// <summary>
	/// Event triggered after clicking maximize or restore button.
	/// </summary>
	public event RoutedEventHandler MaximizeClicked
	{
		add
		{
			AddHandler(MaximizeClickedEvent, value);
		}
		remove
		{
			RemoveHandler(MaximizeClickedEvent, value);
		}
	}

	/// <summary>
	/// Event triggered after clicking minimize button.
	/// </summary>
	public event RoutedEventHandler MinimizeClicked
	{
		add
		{
			AddHandler(MinimizeClickedEvent, value);
		}
		remove
		{
			RemoveHandler(MinimizeClickedEvent, value);
		}
	}

	/// <summary>
	/// Creates a new instance of the class and sets the default <see cref="E:System.Windows.FrameworkElement.Loaded" /> event.
	/// </summary>
	public TitleBar()
	{
		((DependencyObject)this).SetValue(ButtonCommandProperty, (object)new RelayCommand(delegate(object o)
		{
			TemplateButton_OnClick(this, o);
		}));
		base.Loaded += TitleBar_Loaded;
	}

	/// <summary>
	/// Resets icon.
	/// </summary>
	public void ResetIcon()
	{
		if (_notifyIcon != null)
		{
			_notifyIcon.Destroy();
		}
		InitializeNotifyIcon();
	}

	private void CloseWindow()
	{
		if (CloseActionOverride != null)
		{
			CloseActionOverride(this, _parent);
		}
		else if (ApplicationNavigation)
		{
			Application.Current.Shutdown();
		}
		else
		{
			ParentWindow.Close();
		}
	}

	private void MinimizeWindow()
	{
		if (!MinimizeToTray || !UseNotifyIcon || !MinimizeWindowToTray())
		{
			if (MinimizeActionOverride != null)
			{
				MinimizeActionOverride(this, _parent);
			}
			else
			{
				ParentWindow.WindowState = WindowState.Minimized;
			}
		}
	}

	private void MaximizeWindow()
	{
		if (MaximizeActionOverride != null)
		{
			MaximizeActionOverride(this, _parent);
		}
		else if (ParentWindow.WindowState == WindowState.Normal)
		{
			IsMaximized = true;
			ParentWindow.WindowState = WindowState.Maximized;
		}
		else
		{
			IsMaximized = false;
			ParentWindow.WindowState = WindowState.Normal;
		}
	}

	private void InitializeNotifyIcon()
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Expected O, but got Unknown
		if (!DesignerProperties.GetIsInDesignMode(new DependencyObject()))
		{
			NotifyIconClick += OnNotifyIconClick;
			_notifyIcon = new NotifyIcon
			{
				Parent = this,
				Tooltip = NotifyIconTooltip,
				ContextMenu = NotifyIconMenu,
				Icon = NotifyIconImage,
				Click = delegate
				{
					RaiseEvent(new RoutedEventArgs(NotifyIconClickEvent, this));
				},
				DoubleClick = delegate
				{
					RaiseEvent(new RoutedEventArgs(NotifyIconDoubleClickEvent, this));
				}
			};
			_notifyIcon.Show();
		}
	}

	private bool MinimizeWindowToTray()
	{
		if (_notifyIcon == null)
		{
			return false;
		}
		ParentWindow.WindowState = WindowState.Minimized;
		ParentWindow.Hide();
		return true;
	}

	private void OnNotifyIconClick(object sender, RoutedEventArgs e)
	{
		if (MinimizeToTray && ParentWindow.WindowState == WindowState.Minimized)
		{
			ParentWindow.Show();
			ParentWindow.WindowState = WindowState.Normal;
			ParentWindow.Topmost = true;
			ParentWindow.Topmost = false;
			Focus();
		}
	}

	private void InitializeSnapLayout(System.Windows.Controls.Button maximizeButton)
	{
		if (SnapLayout.IsSupported())
		{
			_snapLayout = new SnapLayout();
			_snapLayout.Register(ParentWindow, maximizeButton);
		}
	}

	private void TitleBar_Loaded(object sender, RoutedEventArgs e)
	{
		if (UseNotifyIcon)
		{
			InitializeNotifyIcon();
		}
		System.Windows.Controls.Button button = (System.Windows.Controls.Button)base.Template.FindName("ButtonMaximize", this);
		if (button != null && UseSnapLayout)
		{
			InitializeSnapLayout(button);
		}
		Grid grid = (Grid)base.Template.FindName("RootGrid", this);
		if (grid != null)
		{
			grid.MouseDown += RootGrid_MouseDown;
			grid.MouseLeftButtonDown += RootGrid_MouseLeftButtonDown;
		}
	}

	private void RootGrid_MouseDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ChangedButton == MouseButton.Left)
		{
			ParentWindow.DragMove();
		}
	}

	private void RootGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
	{
		if (e.ClickCount == 2)
		{
			MaximizeWindow();
		}
	}

	private void TemplateButton_OnClick(TitleBar sender, object parameter)
	{
		switch (parameter as string)
		{
		case "close":
			RaiseEvent(new RoutedEventArgs(CloseClickedEvent, this));
			CloseWindow();
			break;
		case "minimize":
			RaiseEvent(new RoutedEventArgs(MinimizeClickedEvent, this));
			MinimizeWindow();
			break;
		case "maximize":
			RaiseEvent(new RoutedEventArgs(MaximizeClickedEvent, this));
			MaximizeWindow();
			break;
		}
	}

	private static void NotifyIconTooltip_OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TitleBar { UseNotifyIcon: not false } titleBar)
		{
			titleBar.ResetIcon();
		}
	}

	private static void UseNotifyIcon_OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is TitleBar titleBar)
		{
			if (titleBar.UseNotifyIcon)
			{
				titleBar.ResetIcon();
			}
			else
			{
				titleBar._notifyIcon.Destroy();
			}
		}
	}

	private static void NotifyIconMenu_OnChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
	}
}
