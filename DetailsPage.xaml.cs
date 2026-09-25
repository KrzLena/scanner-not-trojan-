namespace MauiApp1;

public partial class DetailsPage : ContentPage
{
    private readonly int _recordId;

    private PersonRecord? _record;

    public DetailsPage(int recordId)
    {
        InitializeComponent();

        _recordId = recordId;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _record =
            await DatabaseService
                .GetRecordAsync(_recordId);

        if (_record == null)
            return;

        BindingContext = _record;
    }

    private async void EditClicked(
        object sender,
        EventArgs e)
    {
        if (_record == null)
            return;

        await Navigation.PushAsync(
            new EditPage(
                _record,
                false)
        );
    }

    private async void DeleteClicked(
        object sender,
        EventArgs e)
    {
        if (_record == null)
            return;

        bool answer =
            await DisplayAlert(
                "Usuñ",
                $"Usun¹æ {_record.FullName}?",
                "Tak",
                "Nie"
            );

        if (!answer)
            return;

        await DatabaseService
            .DeleteRecordAsync(_record);

        await Navigation.PopAsync();
    }
}