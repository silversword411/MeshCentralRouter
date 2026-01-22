# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

**MeshCentralRouter** is a Windows desktop application that provides TCP/UDP port mapping and remote access capabilities for the MeshCentral remote management ecosystem. It enables users to establish remote connections (RDP, file transfer, KVM/desktop viewing) to managed devices without requiring VPN configuration.

**License:** Apache 2.0
**Copyright:** 2009-2022 Intel Corporation
**Repository:** https://github.com/silversword411/MeshCentralRouter

## Technology Stack

- **Language:** C# (.NET Framework 4.7.2)
- **UI Framework:** Windows Forms
- **Build System:** MSBuild (Visual Studio 2022 BuildTools)
- **Project Type:** Windows Desktop Application (WinExe)
- **Target Platform:** Windows, AnyCPU (x86/x64)
- **Communication:** WebSocket over SSL/TLS
- **Cryptography:** .NET Framework Security.Cryptography
- **Configuration Storage:** Windows Registry

## Key Commands

### Building

```bash
# Build from VSCode (task configured in .vscode/tasks.json)
Ctrl+Shift+B

# Build from command line
msbuild MeshCentralRouter.sln /p:Configuration=Release

# Debug build
msbuild MeshCentralRouter.sln /p:Configuration=Debug
```

### Running

```bash
# Run debug build
bin\Debug\MeshCentralRouter.exe

# Run release build
bin\Release\MeshCentralRouter.exe
```

### Common Development Tasks

```bash
# The VSCode build task automatically kills running instances before building
# This is configured in .vscode/tasks.json with a pre-launch task
```

## Project Structure

```
MeshCentralRouter/
├── src/                          # Main source code (80 C# files)
│   ├── Forms/                    # UI Form windows
│   │   ├── MainForm.cs          # Primary application window
│   │   ├── KVMViewer.cs         # Remote desktop viewer
│   │   ├── FileViewer.cs        # File transfer interface
│   │   ├── SettingsForm.cs      # Application preferences
│   │   ├── AddPortMapForm.cs    # Port mapping configuration
│   │   ├── AddRelayMapForm.cs   # Relay mapping configuration
│   │   └── [20+ other forms]    # Various dialogs and features
│   │
│   ├── Core Components/
│   │   ├── MeshCentralServer.cs # Server communication
│   │   ├── MeshMapper.cs        # Port mapping management
│   │   ├── WebSocketClient.cs   # WebSocket protocol
│   │   ├── NodeClass.cs         # Device data model
│   │   ├── MeshDiscovery.cs     # Local network discovery
│   │   └── LocalPipe.cs         # Named pipe IPC
│   │
│   ├── Display/Rendering/
│   │   ├── KVMControl.cs        # Desktop rendering control
│   │   ├── KVMResizeControl.cs  # Display resize handling
│   │   ├── ListViewExtended.cs  # Enhanced list view
│   │   ├── DeviceUserControl.cs # Device list item UI
│   │   └── [5+ controls]        # Custom UI controls
│   │
│   ├── Input/Keyboard/Mouse/
│   │   ├── KVMControlHook.cs    # Global keyboard hook
│   │   ├── KVMKeyboardHook.cs   # Keyboard input handling
│   │   └── Win32Api.cs          # Windows API P/Invoke
│   │
│   └── Utilities/
│       ├── Settings.cs          # Registry-based configuration
│       ├── ThemeManager.cs      # Dark/light mode management
│       ├── Translate.cs         # Internationalization
│       ├── MeshUtils.cs         # Utility functions
│       └── WinCrypt.cs          # Windows cryptography APIs
│
├── Properties/
│   ├── AssemblyInfo.cs          # Assembly metadata (v1.10.*)
│   ├── Resources.Designer.cs    # Generated resource file
│   └── Settings.settings        # Application settings
│
├── Resources/                    # Image assets (56+ files)
│   ├── Icons/                   # UI icons in multiple styles
│   └── Graphics/                # Banners, logos, etc.
│
├── Configuration Files
│   ├── MeshCentralRouter.csproj # Visual Studio project
│   ├── MeshCentralRouter.sln    # Visual Studio solution
│   ├── app.config               # Runtime configuration
│   ├── app.manifest             # Windows manifest (UAC)
│   └── MeshCentralRouter-translation.json  # Localization
│
└── README.md                     # Project description
```

## Key Features

1. **Port Mapping** - TCP/UDP port forwarding to remote computers
2. **Remote Desktop (KVM)** - Live desktop viewing and control
3. **File Transfer** - Browse and transfer files to/from remote devices
4. **Device Management** - Organize and manage multiple remote devices
5. **Custom Applications** - Launch custom protocols and applications
6. **Local Network Discovery** - Auto-detect mesh nodes on local networks
7. **Statistics & Monitoring** - Real-time bandwidth and connection stats
8. **Multi-Server Support** - Connect to multiple MeshCentral servers
9. **Internationalization** - Support for 15+ languages
10. **Dark Mode** - Theme customization (recently added)
11. **Password Encryption** - Secure credential storage

## Architecture Overview

### Application Entry Point
- **Program.cs** - Application entry point with single-instance enforcement
- Uses named pipes to ensure only one instance runs
- Parses signature URLs for server locking
- Handles unhandled exceptions globally

### Communication Layer
- **WebSocketClient.cs** - WebSocket protocol with compression, SSL/TLS
- **MeshCentralServer.cs** - Authentication, messaging, device discovery
- JSON-based message protocol over WebSocket
- Event-driven callbacks for server messages

### Configuration & Storage
- **Settings.cs** - Windows Registry storage
  - Path: `HKEY_CURRENT_USER\SOFTWARE\Open Source\MeshCentral Router`
  - Stores: server connections, mappings, preferences, theme
- **ThemeManager.cs** - Singleton dark/light mode management

### UI Architecture
- Windows Forms with custom user controls
- Tab-based main interface (Servers, Devices, Mappings)
- Modal and modeless dialogs for features
- Owner draw components for custom rendering

### Data Models
- **NodeClass** - Represents a managed device
- **MeshClass** - Represents a device group/mesh
- **MeshMapper** - Port mapping session
- **MeshDiscovery** - Local network scanner

## Development Patterns

### Code Style
- Standard C# conventions
- Windows Forms designer pattern (separate .Designer.cs files)
- Event-driven programming model
- Registry-based persistence
- P/Invoke for Windows API access

### Recent Enhancements (dark-mode branch)
The current branch includes recent work on:
- Custom title bar implementation for future Remote Desktop enhancements
- Dark mode theme support
- Password encryption and decryption with persistent storage
- KVMControl refactoring with auto-properties
- Build warnings cleanup

### Common Patterns
1. **Singleton Pattern** - ThemeManager, Settings
2. **Event-Driven** - Server communication, UI updates
3. **P/Invoke** - Windows API integration (Win32Api.cs)
4. **Designer Pattern** - Separate .Designer.cs files for Forms
5. **Registry Storage** - Configuration persistence

### Git Workflow
- **Main branch:** `master`
- **Current branch:** `dark-mode` (active development)
- Feature branches for new development
- Standard Git workflow with GitHub integration

## Important Files to Know

### Core Logic
- [Program.cs](src/Program.cs) - Application entry point
- [MainForm.cs](src/MainForm.cs) - Primary UI window
- [MeshCentralServer.cs](src/MeshCentralServer.cs) - Server communication
- [Settings.cs](src/Settings.cs) - Configuration management

### Remote Access Features
- [KVMViewer.cs](src/KVMViewer.cs) - Remote desktop viewer window
- [KVMControl.cs](src/KVMControl.cs) - Desktop rendering control
- [FileViewer.cs](src/FileViewer.cs) - File transfer UI
- [MeshMapper.cs](src/MeshMapper.cs) - Port mapping logic

### UI Components
- [DeviceUserControl.cs](src/DeviceUserControl.cs) - Device list items
- [MapUserControl.cs](src/MapUserControl.cs) - Mapping list items
- [ServerUserControl.cs](src/ServerUserControl.cs) - Server connection UI
- [ListViewExtended.cs](src/ListViewExtended.cs) - Enhanced list view

### Utilities
- [ThemeManager.cs](src/ThemeManager.cs) - Dark/light theme management
- [Translate.cs](src/Translate.cs) - Internationalization
- [MeshUtils.cs](src/MeshUtils.cs) - Helper functions
- [Win32Api.cs](src/Win32Api.cs) - Windows API declarations

## Configuration

### Build Configuration
- Debug and Release configurations available
- AnyCPU platform (supports x86 and x64)
- .NET Framework 4.7.2 target
- No external NuGet dependencies required

### Runtime Configuration
- [app.config](app.config) - Runtime settings
- [app.manifest](app.manifest) - UAC and Windows compatibility settings
- Registry-based user preferences

### VSCode Integration
- [.vscode/tasks.json](.vscode/tasks.json) - Build task configured
  - Automatically kills running instances before build
  - Uses MSBuild with ProblemMatcher for error reporting
  - Keyboard shortcut: Ctrl+Shift+B

## Localization

The application supports 15+ languages through:
- **Translate.cs** - Built-in translation tables
- **MeshCentralRouter-translation.json** - External translation file

Supported languages include: German, English, Spanish, French, Hindi, Italian, Japanese, Korean, Dutch, Portuguese, Russian, Chinese (Simplified), and more.

## Security Features

- Certificate validation and pinning for server connections
- Windows cryptography APIs for password encryption/decryption
- Secure WebSocket (WSS) connections
- Two-factor authentication support (Email, SMS, messaging)
- Secure credential storage in Windows Registry

## Testing & Debugging

### Running in Debug Mode
- Use Visual Studio or VSCode with C# extension
- Set breakpoints in .cs files
- Debug configuration builds to `bin\Debug\`

### Common Issues
- **Single Instance Enforcement** - Named pipe at `\\.\pipe\MeshCentralRouter` prevents multiple instances
- **Registry Access** - Requires user permissions for `HKEY_CURRENT_USER`
- **WebSocket Connectivity** - Requires network access and valid MeshCentral server

## Resources

- **Documentation:** [MeshCentral Router User Guide (PDF)](http://info.meshcentral.com/downloads/MeshCentral2/MeshCentral2RouterUserGuide-0.0.2.pdf)
- **Website:** [MeshCentral.com](http://www.meshcentral.com)
- **Video Tutorial:** [YouTube Introduction to MeshCentral Router](https://www.youtube.com/watch?v=BubeVRmbCRM)
- **MeshCentral YouTube:** [Channel](https://www.youtube.com/channel/UCJWz607A8EVlkilzcrb-GKg/videos)

## Related Repositories

This repository is part of the MeshCentral ecosystem:
- **MeshCentral** - Node.js remote management server
- **MeshAgent** - C/C++ agent for managed devices
- **MeshCentralRouter** - This Windows port mapping client

These repositories may be opened together in the MeshCentral workspace (`MeshCentral.code-workspace`) for integrated development.

## Statistics

- **Total C# Files:** 80
- **Main Source Files:** 47
- **Lines of Code:** ~19,169
- **Resource Files:** 56+
- **Current Version:** 1.10.* (from AssemblyInfo.cs)

## Notes for Claude Code

When working on this codebase:
- This is a Windows Forms application - changes to UI require updating both .cs and .Designer.cs files
- The project uses registry-based configuration - be careful with Settings.cs changes
- Many forms have designer files - prefer using the designer or Edit tool for UI changes
- The current branch (dark-mode) is under active development for theme features
- WebSocket communication is central to the application - changes to protocol require careful testing
- P/Invoke usage means Windows-specific APIs - cross-platform concerns don't apply
- Single-instance enforcement via named pipes - be aware when debugging multiple instances
