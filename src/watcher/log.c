#include "watcher.h"
#include <stdio.h>
#include <stdarg.h>
#include <wchar.h>

static wchar_t g_logDir[MAX_PATH];
static wchar_t g_logPath[MAX_PATH];

static void BuildPaths(void)
{
    wchar_t localAppData[MAX_PATH] = { 0 };
    DWORD n = GetEnvironmentVariableW(L"LOCALAPPDATA", localAppData, MAX_PATH);
    if (n == 0 || n >= MAX_PATH)
        localAppData[0] = 0;

    swprintf_s(g_logDir, MAX_PATH, L"%s\\PPTBoard\\logs", localAppData);
    CreateDirectoryW(g_logDir, NULL);

    SYSTEMTIME st;
    GetLocalTime(&st);
    swprintf_s(g_logPath, MAX_PATH, L"%s\\watcher-%04d%02d%02d.log",
        g_logDir, st.wYear, st.wMonth, st.wDay);
}

static void CleanupOldLogs(void)
{
    wchar_t pattern[MAX_PATH];
    swprintf_s(pattern, MAX_PATH, L"%s\\watcher-*.log", g_logDir);

    FILETIME nowFileTime;
    SYSTEMTIME now;
    GetSystemTime(&now);
    SystemTimeToFileTime(&now, &nowFileTime);

    ULARGE_INTEGER nowValue;
    nowValue.LowPart = nowFileTime.dwLowDateTime;
    nowValue.HighPart = nowFileTime.dwHighDateTime;
    nowValue.QuadPart -= 7ULL * 24 * 60 * 60 * 10000000ULL;

    WIN32_FIND_DATAW data;
    HANDLE find = FindFirstFileW(pattern, &data);
    if (find == INVALID_HANDLE_VALUE)
        return;

    do
    {
        ULARGE_INTEGER fileValue;
        fileValue.LowPart = data.ftLastWriteTime.dwLowDateTime;
        fileValue.HighPart = data.ftLastWriteTime.dwHighDateTime;
        if (fileValue.QuadPart < nowValue.QuadPart)
        {
            wchar_t full[MAX_PATH];
            swprintf_s(full, MAX_PATH, L"%s\\%s", g_logDir, data.cFileName);
            DeleteFileW(full);
        }
    } while (FindNextFileW(find, &data));

    FindClose(find);
}

void LogInit(void)
{
    BuildPaths();
    CleanupOldLogs();
}

void LogInfo(const wchar_t* format, ...)
{
    wchar_t message[2048];
    va_list args;
    va_start(args, format);
    vswprintf_s(message, 2048, format, args);
    va_end(args);

    SYSTEMTIME st;
    GetLocalTime(&st);

    FILE* file = NULL;
    if (_wfopen_s(&file, g_logPath, L"a, ccs=UTF-8") != 0 || file == NULL)
        return;

    fwprintf(file, L"%04d-%02d-%02d %02d:%02d:%02d.%03d  %s\n",
        st.wYear, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds,
        message);
    fclose(file);
}

void LogClose(void)
{
}
