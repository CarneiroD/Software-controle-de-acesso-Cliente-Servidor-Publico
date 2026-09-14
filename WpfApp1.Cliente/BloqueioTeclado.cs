// WpfApp1.Cliente/BloqueioTeclado.cs
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace WpfApp1.Cliente
{
    // Bloqueia só as teclas mais comuns de troca/minimização de janela.
    // NUNCA intercepta Ctrl+Alt+Del (o Windows não permite) nem
    // Ctrl+Shift+Esc/Ctrl+Esc — Gerenciador de Tarefas e menu Iniciar
    // continuam sempre acessíveis como saída de emergência.
    // O hook é removido automaticamente pelo Windows quando o processo
    // termina, mesmo em caso de crash.
    public static class BloqueioTeclado
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        private const int VK_TAB = 0x09;
        private const int VK_ESCAPE = 0x1B;
        private const int VK_LWIN = 0x5B;
        private const int VK_RWIN = 0x5C;
        private const int VK_F4 = 0x73;
        private const int VK_MENU = 0x12;

        private static IntPtr hookId = IntPtr.Zero;
        private static LowLevelKeyboardProc? callback;

        private delegate IntPtr LowLevelKeyboardProc(
            int nCode,
            IntPtr wParam,
            IntPtr lParam
        );

        public static bool Ativo =>
            hookId != IntPtr.Zero;

        public static void Ativar()
        {
            if (Ativo)
                return;

            callback = HookCallback;

            using Process processoAtual =
                Process.GetCurrentProcess();

            using ProcessModule modulo =
                processoAtual.MainModule!;

            hookId = SetWindowsHookEx(
                WH_KEYBOARD_LL,
                callback,
                GetModuleHandle(modulo.ModuleName),
                0
            );
        }

        public static void Desativar()
        {
            if (!Ativo)
                return;

            UnhookWindowsHookEx(hookId);

            hookId = IntPtr.Zero;

            callback = null;
        }

        private static IntPtr HookCallback(
            int nCode,
            IntPtr wParam,
            IntPtr lParam)
        {
            if (
                nCode >= 0 &&
                (wParam == (IntPtr)WM_KEYDOWN ||
                 wParam == (IntPtr)WM_SYSKEYDOWN)
            )
            {
                int vkCode =
                    Marshal.ReadInt32(lParam);

                bool altPressionado =
                    (GetAsyncKeyState(VK_MENU) & 0x8000) != 0;

                bool combinacaoBloqueada =
                    vkCode == VK_LWIN ||
                    vkCode == VK_RWIN ||
                    (
                        altPressionado &&
                        (
                            vkCode == VK_TAB ||
                            vkCode == VK_ESCAPE ||
                            vkCode == VK_F4
                        )
                    );

                if (combinacaoBloqueada)
                {
                    return (IntPtr)1;
                }
            }

            return CallNextHookEx(
                hookId,
                nCode,
                wParam,
                lParam
            );
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(
            int idHook,
            LowLevelKeyboardProc lpfn,
            IntPtr hMod,
            uint dwThreadId
        );

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(
            IntPtr hhk
        );

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(
            IntPtr hhk,
            int nCode,
            IntPtr wParam,
            IntPtr lParam
        );

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(
            string lpModuleName
        );

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(
            int vKey
        );
    }
}
