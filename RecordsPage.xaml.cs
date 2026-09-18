namespace MauiApp1;

public partial class RecordsPage : ContentPage
{
    private List<PersonRecord> _records = new();

    public RecordsPage()
    {
        InitializeComponent();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        _records = await RecordStore.LoadAsync();

        RecordsView.ItemsSource = null;
        RecordsView.ItemsSource = _records;
    }

    private async void ScanClicked(object sender, EventArgs e)
    {
        var scanPage = new ScanPage();

        await Navigation.PushAsync(scanPage);

       
        Navigation.RemovePage(this);
    }

    private async void AddClicked(object sender, EventArgs e)
    {
        PersonRecord record = new PersonRecord();

        await Navigation.PushAsync(
            new EditPage(record, true)
        );
    }

    private async void RecordTapped(object sender, TappedEventArgs e)
    {
        if (sender is Border border &&
            border.BindingContext is PersonRecord record)
        {
            await Navigation.PushAsync(
                new EditPage(record, false)
            );
        }
    }

    private async void DeleteClicked(object sender, EventArgs e)
    {
        var selected = _records
            .Where(x => x.IsSelected)
            .ToList();

        if (selected.Count == 0)
        {
            return;
        }

        foreach (var record in selected)
        {
            _records.Remove(record);
        }

        await RecordStore.SaveAsync(_records);

        RecordsView.ItemsSource = null;
        RecordsView.ItemsSource = _records;
    }
}