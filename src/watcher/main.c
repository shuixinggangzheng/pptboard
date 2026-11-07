#include "watcher.h"
#include <wchar.h>

#define OVERLAY_EXE L"PPTBoard.exe"
#define WPS_EXE L"wpp.exe"
#define MAX_RESTARTS 3
#define RESTART_DELAY_MS 2000
#define POLL_MS 1000

static wchar_t g_dir[MAX_PATH];
static wchar_t g_overlay[MAX_PATH];

static BOOL BuildPaths(void)
{
    DWORD n = GetModuleFileNameW(NULL, g_dir, MAX_PATH);
    if (n == 0 || n >= MAX_PATH)
        return FALSE;

    wchar_t* slash = wcsrchr(g_dir, L'\\');
    if (slash == NULL)
        return FALSE;
    *slash = L'\0';

    swprintf_s(g_overlay, MAX_PATH, L"%s\\%s", g_dir, OVERLAY_EXE);
    return TRUE;
}

static BOOL StartOverlay(PROCESS_INFORMATION* pi)
{
    STARTUPINFOW si;
    ZeroMemory(&si, sizeof(si));
    si.cb = sizeof(si);
    ZeroMemory(pi, sizeof(*pi));

    return CreateProcessW(g_overlay, NULL, NULL, NULL, FALSE, 0, NULL, g_dir, &si, pi);
}

int WINAPI wWinMain(HINSTANCE instance, HINSTANCE previous, LPWSTR commandLine, int show)
{
    (void)instance;
    (void)previous;
    (void)show;

    HANDLE mutex = CreateMutexW(NULL, TRUE, L"Local\\PPTBoardWatcher_SingleInstance");
    if (mutex == NULL)
        return 1;
    if (GetLastError() == ERROR_ALREADY_EXISTS)
        return 0;

    BuildPaths();
    LogInit();

    BOOL verify = commandLine != NULL && wcsstr(commandLine, L"/verify") != NULL;
    if (verify)
    {
        BOOL found = ProcessExists(WPS_EXE);
        LogInfo(L"verify: wpp.exe=%d", found ? 1 : 0);
        return found ? 0 : 1;
    }

    LogInfo(L"watcher start, overlay=%s", g_overlay);
    if (GetFileAttributesW(g_overlay) == INVALID_FILE_ATTRIBUTES)
        LogInfo(L"WARNING: overlay exe not found");

    HANDLE overlayProcess = NULL;
    int restarts = 0;
    BOOL giveUp = FALSE;

    for (;;)
    {
        BOOL wpsRunning = ProcessExists(WPS_EXE);

        if (wpsRunning && overlayProcess == NULL && !giveUp)
        {
            PROCESS_INFORMATION pi;
            if (StartOverlay(&pi))
            {
                overlayProcess = pi.hProcess;
                CloseHandle(pi.hThread);
                LogInfo(L"overlay launched, pid=%lu, restarts=%d", pi.dwProcessId, restarts);
            }
            else
            {
                LogInfo(L"overlay launch failed, error=%lu", GetLastError());
                giveUp = TRUE;
            }
        }

        if (overlayProcess != NULL)
        {
            DWORD wait = WaitForSingleObject(overlayProcess, 0);
            if (wait == WAIT_OBJECT_0)
            {
                CloseHandle(overlayProcess);
                overlayProcess = NULL;

                if (wpsRunning)
                {
                    if (restarts < MAX_RESTARTS)
                    {
                        restarts++;
                        LogInfo(L"overlay exited while WPS alive, restart %d/%d", restarts, MAX_RESTARTS);
                        Sleep(RESTART_DELAY_MS);
                    }
                    else
                    {
                        giveUp = TRUE;
                        LogInfo(L"overlay failed %d times, giving up until WPS exits", MAX_RESTARTS);
                    }
                }
                else
                {
                    LogInfo(L"overlay exited after WPS closed");
                }
            }
        }

        if (!wpsRunning)
        {
            if (overlayProcess != NULL)
            {
                LogInfo(L"WPS closed, terminating overlay");
                TerminateProcess(overlayProcess, 0);
                CloseHandle(overlayProcess);
                overlayProcess = NULL;
            }
            restarts = 0;
            giveUp = FALSE;
        }

        Sleep(POLL_MS);
    }
}
