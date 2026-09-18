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
            SaveButton.Text = "Dodaj";
        }
        else
        {
            SaveButton.Text = "Zapisz";
        }
    }

    private async void SaveClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(FirstNameEntry.Text) ||
            string.IsNullOrWhiteSpace(LastNameEntry.Text))
        {
            await DisplayAlert(
                "B³¹d",
                "Wpisz imiê i nazwisko.",
                "OK"
            );

            return;
        }

        _record.FirstName = FirstNameEntry.Text.Trim();
        _record.LastName = LastNameEntry.Text.Trim();

        var records = await RecordStore.LoadAsync();

        if (_isNew)
        {
            records.Add(_record);
        }
        else
        {
            var existing = records
                .FirstOrDefault(x => x.Id == _record.Id);

            if (existing != null)
            {
                existing.FirstName = _record.FirstName;
                existing.LastName = _record.LastName;
                existing.QrCode = _record.QrCode;
            }
        }

        await RecordStore.SaveAsync(records);

        await Navigation.PopAsync();
    }

    private async void CancelClicked(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
}