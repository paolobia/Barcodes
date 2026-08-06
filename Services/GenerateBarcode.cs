using Microsoft.JSInterop;

namespace Barcodes.Services
{
    public class GenerateBarcode
    {
        private readonly IJSRuntime _jsRuntime;

        public GenerateBarcode(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string Generate(string data, string tipo )
        {
            if (string.IsNullOrEmpty(data))
                return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg=="; // 1x1 transparent pixel

            if( string.IsNullOrEmpty(tipo))
            {
                data = data.Trim();
                switch (data.Length)
                {
                    case 8:
                        tipo = "EAN8";
                        break;

                    case 12:
                        tipo = "UPC";
                        break;

                    case 13:
                        tipo = "EAN13"; // EAN-13 standard
                        break;

                    case 14:
                        tipo = "ITF14";
                        break;

                    default:
                        tipo =  "CODE128";
                        break;
                }
            }
            else 
                tipo = tipo.ToUpper();
            
            
            try
            {
                // Usa JSInProcessRuntime per chiamate sincrone
                var jsInProcess = (IJSInProcessRuntime)_jsRuntime;
                var result = jsInProcess.Invoke<string>("generateBarcodeSync", data.Trim(), tipo);
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nella generazione del codice a barre: {ex.Message}");
                return "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNkYPhfDwAChwGA60e6kgAAAABJRU5ErkJggg==";
            }
        }
    }
}

