namespace WpfApp1.Core
{
    public static class ConfiguracaoSistema
    {
        // true = usa 20 segundos para testes.
        // false = usa 1 hora.
        public static bool ModoTeste { get; set; } =
            true;

        public static int DuracaoSessaoSegundos
        {
            get
            {
                if (ModoTeste)
                {
                    return 20;
                }

                return 60 * 60;
            }
        }

        public static int DuracaoExtensaoSegundos
        {
            get
            {
                return 30 * 60;
            }
        }
    }
}