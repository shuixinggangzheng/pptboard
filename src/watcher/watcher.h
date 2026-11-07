#pragma once
#include <windows.h>

#ifdef __cplusplus
extern "C" {
#endif

BOOL ProcessExists(const wchar_t* exeName);
void LogInit(void);
void LogInfo(const wchar_t* format, ...);
void LogClose(void);

#ifdef __cplusplus
}
#endif
