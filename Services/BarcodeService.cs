using Barcodes.Models;
using Microsoft.JSInterop;
using System.Text.Json;

namespace Barcodes.Services
{
    public class BarcodeService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string STORAGE_KEY = "barcodes_data";
        private List<BarcodeItem> _barcodes = new();

        public BarcodeService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task<List<BarcodeItem>> GetAllBarcodesAsync()
        {
            if (_barcodes.Count == 0)
            {
                await LoadFromStorageAsync();
            }
            return _barcodes.OrderBy(b => b.Name).ToList();
        }


        public async Task<List<BarcodeItem>> SearchByNameAsync(string searchTerm)
        {
            if (_barcodes.Count == 0)
            {
                await LoadFromStorageAsync();
            }
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                return _barcodes.OrderBy(b => b.Name).ToList();
            }

            return _barcodes
                .Where(b => b.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .OrderBy(b => b.Name)
                .ToList();
        }

        public async Task<BarcodeItem?> SaveBarcodeAsync(BarcodeItem barcode)
        {
            barcode.Name = (barcode.Name ?? string.Empty).Trim().ToUpper(); // maiuscolizziamolo
            barcode.Label = (barcode.Label ?? string.Empty).Trim().ToUpper();
            var existingBarcode = _barcodes.FirstOrDefault(b => b.Name == barcode.Name);
            if (existingBarcode != null)
            {
                existingBarcode.Name = barcode.Name;
                existingBarcode.Code = barcode.Code;
                existingBarcode.Label = barcode.Label;
                existingBarcode.Latitude = barcode.Latitude;
                existingBarcode.Longitude = barcode.Longitude;
                
                await SaveToStorageAsync();
                return existingBarcode;
            }

            _barcodes.Add(barcode);
            await SaveToStorageAsync();
            return barcode;
        }

        public async Task<bool> DeleteBarcodeAsync(string name)
        {
            name = name.ToUpper();
            var barcode = _barcodes.FirstOrDefault(b => b.Name == name);
            if (barcode != null)
            {
                _barcodes.Remove(barcode);
                await SaveToStorageAsync();
                return true;
            }
            return false;
        }

        private async Task LoadFromStorageAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", STORAGE_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    var barcodes = JsonSerializer.Deserialize<List<BarcodeItem>>(json);
                    _barcodes = barcodes ?? new List<BarcodeItem>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel caricamento dei dati: {ex.Message}");
                _barcodes = new List<BarcodeItem>();
            }
        }

        // <summary>
        /// Esporta tutti i codici a barre in formato JSON
        /// </summary>
        /// <returns>Stringa JSON contenente tutti i dati</returns>
        public async Task<string> ExportToJsonAsync()
        {
            if (_barcodes.Count == 0)
            {
                await LoadFromStorageAsync();
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            
            try
            {
                return JsonSerializer.Serialize(_barcodes, options);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'esportazione: {ex.Message}");
                throw new InvalidOperationException("Errore durante l'esportazione dei dati", ex);
            }
        }


        /// <summary>
        /// Importa codici a barre da una stringa JSON
        /// </summary>
        /// <param name="jsonData">Stringa JSON contenente i dati da importare</param>
        /// <param name="replaceExisting">Se true, sostituisce tutti i dati esistenti. Se false, aggiunge ai dati esistenti</param>
        /// <returns>Numero di elementi importati</returns>
        public async Task<int> ImportFromJsonAsync(string jsonData, bool replaceExisting )
        {
            if (string.IsNullOrWhiteSpace(jsonData))
            {
                throw new ArgumentException("I dati JSON non possono essere vuoti", nameof(jsonData));
            }

            try
            {
                // Carica i dati esistenti se necessario
                if (_barcodes.Count == 0)
                {
                    await LoadFromStorageAsync();
                }

                // Prova prima il formato con metadati (esportazione completa)
                var importedBarcodes = new List<BarcodeItem>();
                
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,  // Importante per la deserializzazione
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                };
                var barcodes = JsonSerializer.Deserialize<List<BarcodeItem>>(jsonData, options);
                if (barcodes != null)
                {
                    importedBarcodes = barcodes;
                }
               
                if (!importedBarcodes.Any())
                {
                    throw new InvalidOperationException("Nessun dato valido trovato nel file JSON");
                }

                // Validazione e normalizzazione dei dati importati
                var validBarcodes = new List<BarcodeItem>();
                foreach (var barcode in importedBarcodes)
                {
                    if (string.IsNullOrWhiteSpace(barcode.Name) || string.IsNullOrWhiteSpace(barcode.Code))
                    {
                        Console.WriteLine($"Saltato elemento non valido: Nome='{barcode.Name}', Codice='{barcode.Code}'");
                        continue;
                    }

                    barcode.Name = barcode.Name.Trim().ToUpper();
                    barcode.Label = (barcode.Label ?? string.Empty).Trim().ToUpper();
                    validBarcodes.Add(barcode);
                }

                if (!validBarcodes.Any())
                {
                    throw new InvalidOperationException("Nessun elemento valido trovato nei dati importati");
                }

                // Gestione duplicati e importazione
                int importedCount = 0;

                if (replaceExisting)
                {
                    // Sostituisce tutti i dati
                    _barcodes.Clear();
                    _barcodes.AddRange(validBarcodes);
                    importedCount = validBarcodes.Count;
                }
                else
                {
                    // Aggiunge ai dati esistenti, gestendo i duplicati
                    foreach (var newBarcode in validBarcodes)
                    {
                        var existingByName = _barcodes.FirstOrDefault(b => b.Name.Equals(newBarcode.Name, StringComparison.OrdinalIgnoreCase));

                        if (existingByName != null)
                        {
                            // Aggiorna quello esistente con stesso Nome
                            existingByName.Code = newBarcode.Code;
                            existingByName.Label = newBarcode.Label;
                            existingByName.Latitude = newBarcode.Latitude;
                            existingByName.Longitude = newBarcode.Longitude;
                            importedCount++;
                        }
                        else
                        {
                            // Nuovo elemento
                            _barcodes.Add(newBarcode);
                            importedCount++;
                        }
                    }
                }

                // Salva i dati aggiornati
                await SaveToStorageAsync();
                
                return importedCount;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"Errore nella deserializzazione JSON: {ex.Message}");
                throw new InvalidOperationException($"Il file JSON non è in un formato valido {ex.Message} {jsonData}", ex);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nell'importazione: {ex.Message}");
                throw new InvalidOperationException($"Errore durante l'importazione dei dati {ex.Message} {jsonData}", ex);
            }
        }

        private async Task SaveToStorageAsync()
        {
            try
            {
                var json = JsonSerializer.Serialize(_barcodes);
                await _jsRuntime.InvokeVoidAsync("localStorage.setItem", STORAGE_KEY, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Errore nel salvataggio dei dati: {ex.Message} ");
            }
        }
    }
}