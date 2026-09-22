using System.Globalization;
using System.Text.RegularExpressions;
using ZXing.Net.Maui;

namespace MauiApp1;

public partial class ScanPage : ContentPage
{
    private bool _canScan = true;

    public ScanPage()
    {
        InitializeComponent();

        CameraView.Options =
            new BarcodeReaderOptions
            {
                Formats = BarcodeFormats.TwoDimensional,
                AutoRotate = true,
                Multiple = false
            };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _canScan = true;

        var permission =
            await Permissions.RequestAsync<Permissions.Camera>();

        if (permission != PermissionStatus.Granted)
        {
            await DisplayAlert(
                "Kamera",
                "Aplikacja potrzebuje dostępu do kamery.",
                "OK"
            );
        }
    }

    private void BarcodesDetected(
        object sender,
        BarcodeDetectionEventArgs e)
    {
        if (!_canScan)
            return;

        var result =
            e.Results.FirstOrDefault();

        if (result == null)
            return;

        _canScan = false;

        MainThread.BeginInvokeOnMainThread(
            async () =>
            {
                string text =
                    NormalizeName(result.Value);

                ScannedCodeLabel.Text = text;

                string[] dane =
                    text.Split(
                        ' ',
                        2,
                        StringSplitOptions.RemoveEmptyEntries
                    );

                if (dane.Length < 2)
                {
                    await DisplayAlert(
                        "Błąd",
                        "Kod musi zawierać imię i nazwisko.",
                        "OK"
                    );

                    _canScan = true;
                    return;
                }

                string firstName = dane[0];
                string lastName = dane[1];

                var duplicate =
                    await DatabaseService
                        .FindDuplicateAsync(
                            text,
                            firstName,
                            lastName
                        );

                if (duplicate != null)
                {
                    string lastScan =
                        duplicate.LastScannedAt.HasValue
                            ? duplicate.LastScannedAt.Value
                                .ToString("dd.MM.yyyy HH:mm")
                            : "Nigdy";

                    bool update =
                        await DisplayAlert(
                            "Osoba już istnieje",
                            duplicate.FullName +
                            "\n\nOstatni skan:\n" +
                            lastScan,
                            "Aktualizuj datę",
                            "Anuluj"
                        );

                    if (!update)
                    {
                        _canScan = true;
                        return;
                    }

                    duplicate.LastScannedAt =
                        DateTime.Now;

                    await DatabaseService
                        .UpdateRecordAsync(duplicate);

                    await DisplayAlert(
                        "Zaktualizowano",
                        duplicate.FullName,
                        "OK"
                    );

                    await OpenList();

                    return;
                }

                PersonRecord record =
                    new PersonRecord
                    {
                        FirstName = firstName,
                        LastName = lastName,
                        QrCode = text,
                        CreatedAt = DateTime.Now,
                        LastScannedAt = DateTime.Now,
                        IsManual = false
                    };

                await DatabaseService
                    .AddRecordAsync(record);

                await DisplayAlert(
                    "Dodano",
                    "Dodano: " + record.FullName,
                    "OK"
                );

                await OpenList();
            });
    }

    private static string NormalizeName(string value)
    {
        value = Regex.Replace(
            value.Trim(),
            @"\s+",
            " "
        );

        var culture =
            CultureInfo.GetCultureInfo("pl-PL");

        return culture.TextInfo.ToTitleCase(
            value.ToLower(culture)
        );
    }

    private async Task OpenList()
    {
        var page = new RecordsPage();

        await Navigation.PushAsync(page);

        Navigation.RemovePage(this);
    }

    private void ScanClicked(object sender, EventArgs e)
    {
        ScannedCodeLabel.Text = "";
        _canScan = true;
    }

    private async void ListClicked(object sender, EventArgs e)
    {
        await OpenList();
    }
}
