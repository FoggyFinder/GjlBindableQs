open FsXaml
open System
open System.Windows

type MainWindowBase = XAML<"MainWindow.xaml">

type MainWindow() as self =
    inherit MainWindowBase()

    do self.submitButton2.Click.Add (fun _ -> MessageBox.Show("Hello world2!") |> ignore)

    override this.submitButton_Click (s: obj, e: RoutedEventArgs) = 
        MessageBox.Show("Hello world!")
        |> ignore

[<STAThread>]
MainWindow()
|> Application().Run
|> ignore