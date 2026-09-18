using ZXing.Net.Maui;

namespace MauiApp1;

public partial class ScanPage : ContentPage
{
    private bool _canScan = true;

    public ScanPage()
    {
        InitializeComponent();

        CameraView.Options = new BarcodeReaderOptions
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
                "Aplikacja potrzebuje dostêpu do kamery.",
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

        var result = e.Results.FirstOrDefault();

        if (result == null)
            return;

        _canScan = false;

        MainThread.BeginInvokeOnMainThread(async () =>
        {
            string text = result.Value.Trim();

            ScannedCodeLabel.Text = text;

            string[] dane = text.Split(
                ' ',
                2,
                StringSplitOptions.RemoveEmptyEntries
            );

            if (dane.Length < 2)
            {
                await DisplayAlert(
                    "B³¹d",
                    "Kod QR musi zawieraæ imiê i nazwisko",
                    "OK"
                );

                _canScan = true;
                return;
            }

            PersonRecord record = new PersonRecord
            {
                FirstName = dane[0],
                LastName = dane[1],
                QrCode = text
            };

            var records = await RecordStore.LoadAsync();

            records.Add(record);

            await RecordStore.SaveAsync(records);

            await DisplayAlert(
                "Dodano",
                "Dodano: " + record.FullName,
                "OK"
            );

            var recordsPage = new RecordsPage();

            await Navigation.PushAsync(recordsPage);
            Navigation.RemovePage(this);
        });
    }

    private void ScanClicked(object sender, EventArgs e)
    {
        ScannedCodeLabel.Text = "";
        _canScan = true;
    }

    private async void ListClicked(object sender, EventArgs e)
    {
        var recordsPage = new RecordsPage();

        await Navigation.PushAsync(recordsPage);

        Navigation.RemovePage(this);
    }
}