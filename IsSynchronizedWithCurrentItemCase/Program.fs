open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml

    type MainWindow = XAML<"MainWindow.xaml">

open Gjallarhorn.Wpf
open Gjallarhorn.Bindable
open Gjallarhorn.Bindable.Framework

type SomeType = {
    AllData : string
    Current : char
}
with member x.TestData = x.AllData |> Seq.toArray

[<RequireQualifiedAccess>]
type SomeMessages = | SetCurrent of char

let update message model =
    match message with
    | SomeMessages.SetCurrent c ->
        printfn "setCurrent"
        { model with Current = c }

let allData = "test string"
let init = { AllData = allData; Current = allData.[0] }

let appComp : IComponent<SomeType, unit, SomeMessages> =
    Component.create<SomeType, unit, SomeMessages> [
        <@ init.Current @> |> Bind.twoWay (fun m -> m.Current) SomeMessages.SetCurrent
        <@ init.TestData @> |> Bind.oneWay (fun m -> m.TestData)
    ]

let applicationCore nav = Framework.application init update appComp nav

[<EntryPoint>]
[<STAThread>]
let main _ = 
    let navigator = Navigation.singleViewFromWindow Views.MainWindow

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0