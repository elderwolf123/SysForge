# Ram Optimizer

A comprehensive system optimization tool for Windows devices with limited RAM, designed to maximize performance by intelligently managing system resources.

## Features

### Ultra-Aggressive Process Termination Engine
- 7 levels of process termination with increasing aggression
- Safety mechanisms to protect critical system processes
- Dynamic exclusion list that learns from system behavior
- Process restoration when target applications close

### Advanced File Compression System
- Transparent real-time compression with zero performance impact
- Custom hybrid compression algorithms (lossless + near-lossless)
- Automatic background compression of inactive files
- Intelligent file type detection and algorithm selection
- Compression ratio optimization based on usage patterns
- Delta compression for similar files
- Predictive compression based on file access patterns
- Memory-mapped compression for large files
- Real-time compression statistics and savings tracking

### CPU Optimization
- CPU usage pattern monitoring and optimization
- Dynamic frequency scaling and power management
- Process priority adjustment for target applications
- CPU affinity management for optimal core allocation
- Thermal management to prevent throttling
- Performance core allocation for modern CPUs

### GPU Optimization
- GPU resource allocation optimization
- Hardware acceleration management
- GPU scheduler optimization
- VRAM allocation optimization
- Aggression levels for non-critical processes

### System Monitoring
- Real-time performance monitoring (CPU/GPU/RAM displays)
- System tray functionality for lightweight operation
- Comprehensive error handling and logging system
- System stability testing framework

### Battery Power Management
- 3-tier power optimization modes (Performance/Balanced/Maximum Battery)
- Intelligent power mode switching based on battery level
- CPU/GPU power throttling with performance scaling
- Background service optimization for battery conservation
- Display and hardware power management

## Architecture

The application follows a modular architecture with separate components for:

1. **Process Management** - Handles process termination and restoration
2. **File Compression** - Manages file compression and decompression
3. **CPU Optimization** - Optimizes CPU usage and resource allocation
4. **GPU Optimization** - Manages GPU resources and optimization
5. **System Monitoring** - Provides real-time system metrics
6. **UI Layer** - User interface components (WPF)

## Installation

1. Clone the repository
2. Open the solution in Visual Studio
3. Build the solution
4. Run the application

## Usage

### Console Mode
```
cd src\ProcessManagement
dotnet run --console
```

### GUI Mode
```
cd RamOptimizerUI
dotnet run
```

### System Tray Mode
```
cd src\ProcessManagement
dotnet run --tray
```

## Requirements

- Windows 10 or later
- .NET 7.0
- Visual Studio 2022 or later

## Building

Run the build script:
```
build.bat
```

## Running

Run the application:
```
run.bat
```

## Testing

The project includes comprehensive unit tests for all major components:
- Process termination engine tests
- System stability tests
- File compression tests
- CPU optimization tests
- GPU optimization tests

Run tests with:
```
dotnet test
```

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Contributing

1. Fork the repository
2. Create a feature branch
3. Commit your changes
4. Push to the branch
5. Create a pull request