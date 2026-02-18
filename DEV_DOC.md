# FocusPanel Development Documentation

This document serves as a guide for developers working on the FocusPanel project. It covers the architecture, project structure, and key implementation details.

## Project Structure

The project follows a standard MVVM architecture, organized into the following directories:

-   **`Views/`**: Contains XAML files and their code-behind.
    -   `MainWindow.xaml`: The main shell of the application (Full-screen overlay).
    -   `DashboardView.xaml`, `TasksView.xaml`, etc.: Content views for specific modules.
-   **`ViewModels/`**: Contains the application logic and state.
    -   `MainViewModel.cs`: Handles top-level navigation and window commands.
    -   `TasksViewModel.cs`: Manages task list logic.
-   **`Models/`**: Contains data entities (POCOs).
    -   `TodoItem.cs`: Represents a task entity.
-   **`Services/`**: Contains business logic and data access layers.
    -   `TaskService.cs`: Handles CRUD operations for tasks using EF Core.
-   **`Data/`**: Contains database context and configuration.
    -   `AppDbContext.cs`: EF Core DbContext for SQLite.
-   **`Converters/`**: Contains WPF ValueConverters.
    -   `BooleanToStrikeThroughConverter.cs`: UI converter for task completion style.

## Architecture & Patterns

### MVVM (Model-View-ViewModel)
We use the **CommunityToolkit.Mvvm** library for boilerplate reduction.
-   ViewModels inherit from `ObservableObject`.
-   Properties use `[ObservableProperty]` for automatic `INotifyPropertyChanged` implementation.
-   Commands use `[RelayCommand]`.

### Dependency Injection (DI)
Currently, the project uses a simple instantiation approach in `MainViewModel` for simplicity. For scaling, consider introducing a DI container (e.g., `Microsoft.Extensions.DependencyInjection`) in `App.xaml.cs`.

### Database
-   **SQLite** is used as the local database.
-   **Entity Framework Core** is used as the ORM.
-   The database file `focuspanel.db` is created in the application's working directory on startup (`App.xaml.cs` -> `EnsureCreated`).

## Key Features Implementation

### Desktop Overlay
`MainWindow.xaml` is configured to be a full-screen transparent window:
```xml
<Window ...
    Background="Transparent"
    AllowsTransparency="True"
    WindowStyle="None"
    WindowState="Maximized"
    ResizeMode="CanResizeWithGrip">
```
This allows the wallpaper to show through while widgets (Clock, Dock) remain visible.

### Hover-Expand Sidebar
The sidebar logic is handled in `MainWindow.xaml.cs` using WPF Animations (`DoubleAnimation`) triggered by `MouseEnter` and `MouseLeave` events on the `SidebarBorder`. This ensures smooth transitions between the collapsed (80px) and expanded (800px) states.

## Adding a New Module

1.  **Create View**: Add `NewModuleView.xaml` in `Views/`.
2.  **Create ViewModel**: Add `NewModuleViewModel.cs` in `ViewModels/`.
3.  **Register DataTemplate**: Add a `DataTemplate` mapping in `MainWindow.xaml` resources.
4.  **Add Navigation**: Update `MainViewModel.cs` to include a navigation case for the new module.
5.  **Add Button**: Add a button in the `MainWindow.xaml` sidebar to trigger the navigation.

## Dependencies

-   `CommunityToolkit.Mvvm`: MVVM support.
-   `MaterialDesignThemes`: UI components and styling.
-   `Hardcodet.NotifyIcon.Wpf`: System tray support.
-   `Microsoft.EntityFrameworkCore.Sqlite`: Database provider.

## Troubleshooting

-   **Build Errors with MaterialDesign**: Ensure you are using a compatible version (currently downgraded to 4.9.0 for stability).
-   **Database Locks**: If `focuspanel.db` is locked, ensure the application is fully closed (check Task Manager) before trying to delete or move it.
