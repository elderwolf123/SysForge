# Ultra Aggressive Safety Design

## Safety Guarantees

The ultra-aggressive design maximizes RAM optimization while maintaining system stability through continuous monitoring, intelligent recovery, and methodical progression through termination levels. The system performs stability testing after each process termination. Recovery mechanisms are initiated if the system becomes unstable. The optimization state is persisted to allow the process to continue after a reboot.

## Absolute Protection List

The system includes an absolute protection list that contains critical system processes that should never be terminated. The list includes processes such as `kernel32.dll`, `ntoskrnl.exe`, `hal.dll`, `win32k.sys`, `csrss.exe`, `winlogon.exe`, `services.exe`, `lsass.exe`, `smss.exe`, and `wininit.exe`. These processes are excluded from termination to ensure system stability.