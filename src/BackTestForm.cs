using System;
using System.Windows.Forms;

namespace Arbitrader
{
    public partial class BackTestForm : Form
    {
        public BackTestForm(AxKHOpenAPILib.AxKHOpenAPI openApi)
        {
            InitializeComponent();
            OpenApi = openApi;

            var list = OpenApi.GetCodeListByMarket("8").Trim().Split(';');
            Debug.Info(list);
        }

        private void BackTestForm_Load(object sender, EventArgs e)
        {
        }

        private AxKHOpenAPILib.AxKHOpenAPI OpenApi;
    }
}
