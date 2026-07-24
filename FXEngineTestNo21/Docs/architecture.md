# FX Engine Architecture

## Core Principles

- Modularity: every manager is isolated and replaceable.
- Interfaces first: applications, plugins, themes, packages, and renderers rely on contracts.
- Serializable settings: all settings are stored as JSON.
- Extensibility: new applications can be added without modifying existing engine code.

## Manager Responsibilities

- EngineManager: coordinates engine startup and shutdown.
- ThemeManager: registers and applies themes.
- PluginManager: registers and initializes plugins.
- PackageManager: tracks installable and installed packages.
- ProfileManager: loads and saves user profiles.
- LayoutManager: saves and restores layouts.
- ApplicationManager: registers and launches applications.
- SettingsManager: stores engine-wide settings in JSON.
- EventManager: publishes engine events to subscribers.
- ServiceManager: exposes engine services through a registry.
- RendererManager: manages renderer implementations.
- AssetManager: ensures and resolves asset paths.
