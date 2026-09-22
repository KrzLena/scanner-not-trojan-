using System.Globalization;
using System.Text.RegularExpressions;

namespace MauiApp1;

public partial class EditPage : ContentPage
{
    private readonly PersonRecord _record;
    private readonly bool _isNew;

    public EditPage(PersonRecord record, bool isNew)
    {
        InitializeComponent();

        _record = record;
        _isNew = isNew;

        FirstNameEntry.Text = record.FirstName;
        LastNameEntry.Text = record.LastName;

        if (_isNew)
        {
            Title = "Dodaj osobę";
            SaveButton.Text = "Dodaj";
        }
        else
        {
            Title = "Edytuj osobę";
            SaveButton.Text = "Zapisz";
        }
    }

    private async void SaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(LastNameEntry.Text))
        {
            await DisplayAlert(
                "Błąd",
                "Wpisz imię i nazwisko.",
                "OK"
            );

            return;
        }

        _record.FirstName =
            NormalizeName(FirstNameEntry.Text);

        _record.LastName =
            NormalizeName(LastNameEntry.Text);

        if (_isNew)
        {
            _record.CreatedAt = DateTime.Now;
            _record.IsManual = true;
            _record.QrCode = _record.FullName;

            await DatabaseService.AddRecordAsync(_record);
        }
        else
        {
            if (_record.IsManual)
            {
                _record.QrCode = _record.FullName;
            }

            await DatabaseService.UpdateRecordAsync(_record);
        }

        await Navigation.PopAsync();
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

    private async void CancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}
