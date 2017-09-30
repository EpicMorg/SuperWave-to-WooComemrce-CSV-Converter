using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;

namespace sw2wc {
    public partial class FrmMain : Form  {
        public FrmMain() {
            InitializeComponent();
        }

        /* 
           Это список всех катерогий. Скаждой строки новая. 
        
            HVD > Запасные части Xerox HVD
            HVD > Оборудование Xerox HVD > Мониторы и сканеры Xerox
            HVD > Оборудование Xerox HVD > МФУ Xerox A3 
            HVD > Оборудование Xerox HVD > МФУ Xerox A4 Color
            HVD > Оборудование Xerox HVD > МФУ Xerox A4 Mono 
            HVD > Оборудование Xerox HVD > Принтеры Xerox Color 
            HVD > Оборудование Xerox HVD > Принтеры Xerox Mono 
            HVD > Расходные материалы Xerox HVD 
            HVD > Расходные материалы XRC 
            GMO > Запасные части Xerox GMO
            GMO > Оборудование Xerox GMO > Конвертовальные системы Xerox
            GMO > Оборудование Xerox GMO > Офисное оборудование Xerox монохромное
            GMO > Оборудование Xerox GMO > Офисное оборудование Xerox цветное
            GMO > Оборудование Xerox GMO > Программное обеспечение Xerox
            GMO > Оборудование Xerox GMO > Широкоформатное оборудование Xerox
            GMO > Расходные материалы Xerox GMO

            А это формат CSV для экспорта: 
            Артикул,Имя,"Короткое описание",Описание,"В наличии?","Базовая цена",Категории
            string,string,string,string,int,int,string

        */

        #region Методы доступа к атрибутам сборки
        private static string GetAssemblyVersion() {
            return Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }

        private string GetAssemblyDescription() {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyDescriptionAttribute)attributes[0]).Description;
        }

        private static string GetAssemblyCopyright() {
            var attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
            return attributes.Length == 0 ? "" : ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
        }
        #endregion

        private void FrmMain_Load(object sender, EventArgs e) {
            comboBoxCategory.SelectedIndex = 0;
            this.Text = Text + @", " + GetAssemblyVersion() + @", " + GetAssemblyCopyright();
        }
        private void listBox_DragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, false) == true) {
                e.Effect = DragDropEffects.All;
            }
        }

        public void listBox_DragDrop(object sender, DragEventArgs e) {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            foreach (var file in files) {
                string input = null; 
                using (StreamReader sr = new StreamReader(file, Encoding.GetEncoding(1251))) {
                    sr.ReadLine();
                    while((input = sr.ReadLine()) != null)
                    listBox.Items.Add(input);
                }
            }
            checkBoxStatus.Checked = true;
        }

        private void comboBoxCategory_SelectedIndexChanged(object sender, EventArgs e) {
            labelSelectedCat.Text = comboBoxCategory.Text;
        }

        private void buttonConvert_Click(object sender, EventArgs e){
            StringBuilder sb = new StringBuilder();
            XmlDocument xDoc = new XmlDocument();
            try  {
                xDoc.Load("https://www.cbr-xml-daily.ru/daily_utf8.xml");
            } catch (Exception ex) {
                MessageBox.Show(@"Не могу открыть URL с курсом валют." + Environment.NewLine
                                + ex, @"Ошибка!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            XmlElement xRoot = xDoc.DocumentElement;
            double USD = 0;
            foreach (XmlNode xn in xRoot.ChildNodes){
                if (xn.Attributes[0].Value.Contains("R01235")) {
                    USD = Double.Parse(xn.ChildNodes[4].InnerText);
                }
            }
            NumberFormatInfo provider = new NumberFormatInfo();
            provider.NumberDecimalSeparator = ".";
            foreach (var item in listBox.Items) {
                string[] csv = ((string) item).Split(';');
                for (int i = 0; i < csv.Length; i++) {
                    csv[i] = csv[i].Trim('"', '=');
                }
                string price = csv[7];
                if (price.IndexOf("руб") < 0) {
                    price = price.Trim(' ', '$');
                    //if (price == "нет") continue; // это если кто-то будет писать НЕТ в значение инта..
                    double result = 0;
                    Double.TryParse(price, NumberStyles.Any, provider, out result);
                    price = (result * USD).ToString();
                } else {
                    price = price.Replace("руб", "").Trim('.', ' ', '"', '=');
                }
                price = Math.Round(((double)numericUpDown1.Value + 100) * Convert.ToDouble(price) / 100, 0, MidpointRounding.AwayFromZero).ToString();
                // воркэраунд от народа, которые ставят цену 0 и количество товара больше 0. wtf
                if (price == "0") {
                    csv[3] = "0";
                }
                sb.AppendLine($"{csv[0]},\"{csv[1]}\",\"{csv[1]}\",\"{csv[1]}\",{csv[3]},{price},\"{comboBoxCategory.Text.Trim(' ')}\"");
            }
            saveFileDialog.ShowDialog();
            using (StreamWriter sw = new StreamWriter(saveFileDialog.FileName)) {
                sb.Insert(0, "Артикул,Имя,\"Короткое описание\",Описание,\"В наличии?\",\"Базовая цена\",\"Категории\"\n");
                sw.WriteLine(sb.ToString());
                MessageBox.Show(@"Работа завершена! Программма будет перезапущена.",@"Готово",MessageBoxButtons.OK,MessageBoxIcon.Information);
                Application.Restart();
            }
        }
    }
}
