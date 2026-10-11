using System;
using System.Windows;
using System.Windows.Controls;

namespace DesktopCat
{
    public class PersonalityWindow : Window
    {
        readonly Slider[] sliders=new Slider[8];
        readonly CheckBox attention=new CheckBox();
        readonly ComboBox presets=new ComboBox();
        readonly Slider size=new Slider {Minimum=.5,Maximum=2,TickFrequency=.05,IsSnapToTickEnabled=true,SmallChange=.05,LargeChange=.25};
        bool loading;
        string presetName;
        readonly Action<Personality> apply;
        public PersonalityWindow(Personality initial,Action<Personality> onSave,bool recovered)
        {
            apply=onSave;
            Title="Cat personality & size";Width=460;Height=660;MinWidth=390;MinHeight=350;
            WindowStartupLocation=WindowStartupLocation.CenterScreen;ShowInTaskbar=true;
            PreviewKeyDown+=delegate(object sender,System.Windows.Input.KeyEventArgs e){if(e.Key==System.Windows.Input.Key.Escape){e.Handled=true;Close();}};
            var shell=new DockPanel {Margin=new Thickness(18)};
            var buttons=new StackPanel {Orientation=Orientation.Horizontal,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,15,0,0)};
            DockPanel.SetDock(buttons,Dock.Bottom);shell.Children.Add(buttons);
            var save=new Button {Content="Save",Width=90,Height=30,Margin=new Thickness(0,0,8,0),IsDefault=true};
            var cancel=new Button {Content="Cancel",Width=90,Height=30,IsCancel=true};
            save.Click+=delegate {Save();};cancel.Click+=delegate {Close();};buttons.Children.Add(save);buttons.Children.Add(cancel);
            var content=new StackPanel();shell.Children.Add(new ScrollViewer {Content=content,VerticalScrollBarVisibility=ScrollBarVisibility.Auto});
            content.Children.Add(new TextBlock {Text="The cat keeps moving while you adjust these settings.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
            if(recovered)content.Children.Add(new TextBlock {Text="Saved settings could not be read. Defaults are shown; the old file will be backed up when you save.",TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,0,0,12)});
            content.Children.Add(new TextBlock {Text="Personality preset",Margin=new Thickness(0,0,0,5)});
            foreach(string name in Personality.Names)presets.Items.Add(name);
            presets.Items.Add("Custom");content.Children.Add(presets);
            string[] names={"Playfulness","Sleepiness","Curiosity","Affection","Boldness","Sensitivity","Patience","Movement speed (%)"};
            for(int i=0;i<8;i++)
            {
                var row=new Grid {Margin=new Thickness(0,9,0,0)};
                row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(160)});
                row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(1,GridUnitType.Star)});
                row.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(35)});
                row.Children.Add(new TextBlock {Text=names[i],VerticalAlignment=VerticalAlignment.Center});
                var slider=new Slider {Minimum=i==7?50:0,Maximum=i==7?150:100,TickFrequency=1,IsSnapToTickEnabled=true,Margin=new Thickness(3,0,10,0)};
                var number=new TextBlock {HorizontalAlignment=HorizontalAlignment.Right,VerticalAlignment=VerticalAlignment.Center};
                Grid.SetColumn(slider,1);Grid.SetColumn(number,2);row.Children.Add(slider);row.Children.Add(number);content.Children.Add(row);
                sliders[i]=slider;
                slider.ValueChanged+=delegate {number.Text=slider.Value.ToString("0");if(!loading){presetName="Custom";presets.SelectedIndex=Personality.Names.Length;}};
            }
            attention.Content="Notice and interact with the cursor";attention.Margin=new Thickness(0,18,0,0);content.Children.Add(attention);
            var sizeRow=new Grid {Margin=new Thickness(0,18,0,0)};
            sizeRow.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(160)});
            sizeRow.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(1,GridUnitType.Star)});
            sizeRow.ColumnDefinitions.Add(new ColumnDefinition {Width=new GridLength(55)});
            sizeRow.Children.Add(new TextBlock {Text="Cat size",VerticalAlignment=VerticalAlignment.Center});
            var sizeLabel=new TextBlock {Text=size.Value.ToString("0.00")+"×",VerticalAlignment=VerticalAlignment.Center,HorizontalAlignment=HorizontalAlignment.Right};
            size.Margin=new Thickness(3,0,10,0);Grid.SetColumn(size,1);Grid.SetColumn(sizeLabel,2);
            size.ValueChanged+=delegate {sizeLabel.Text=size.Value.ToString("0.00")+"×";};
            sizeRow.Children.Add(size);sizeRow.Children.Add(sizeLabel);content.Children.Add(sizeRow);
            content.Children.Add(new TextBlock {Text="0.50×–2.00× · Default 1.25×. Size applies when you save.",Margin=new Thickness(0,6,0,0),TextWrapping=TextWrapping.Wrap});
            presets.SelectionChanged+=delegate {
                if(loading || presets.SelectedIndex<0 || presets.SelectedIndex>=Personality.Names.Length)return;
                bool enabled=attention.IsChecked==true;double chosenSize=size.Value;
                LoadValues(Personality.ForPreset(presets.SelectedIndex));attention.IsChecked=enabled;size.Value=chosenSize;
            };
            Content=shell;LoadValues(initial);
        }
        void LoadValues(Personality p)
        {
            loading=true;
            double[] values={p.Playfulness,p.Sleepiness,p.Curiosity,p.Affection,p.Boldness,p.Sensitivity,p.Patience,p.Speed*100};
            for(int i=0;i<8;i++)sliders[i].Value=values[i];
            attention.IsChecked=p.CursorAttention;presetName=p.Preset;
            size.Value=p.PetSize;
            int index=Array.IndexOf(Personality.Names,p.Preset);presets.SelectedIndex=index<0?Personality.Names.Length:index;
            loading=false;
        }
        void Save()
        {
            var value=new Personality {Preset=presetName,Playfulness=sliders[0].Value,Sleepiness=sliders[1].Value,Curiosity=sliders[2].Value,Affection=sliders[3].Value,Boldness=sliders[4].Value,Sensitivity=sliders[5].Value,Patience=sliders[6].Value,Speed=sliders[7].Value/100,CursorAttention=attention.IsChecked==true};
            value.PetSize=size.Value;value.Validate();apply(value);Close();
        }
        // Reuse the actual controls/Save handler in the integration probe.
        internal void SelectPresetForTest(int index){presets.SelectedIndex=index;}
        internal void SaveForTest(){Save();}
        internal void SetSizeForTest(double value){size.Value=value;}
        internal double SizeForTest { get { return size.Value; } }
    }
}
