open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml

    type MainWindow = XAML<"MainWindow.xaml">
    type Page1 = XAML<"Page1.xaml">

    type Page2 = XAML<"Page2.xaml">

open Gjallarhorn.Wpf
open Gjallarhorn.Bindable
open Gjallarhorn.Validation

[<RequireQualifiedAccess>]
type NavMessages = 
    | Page1
    | Page2

[<RequireQualifiedAccess>]
type Messages = 
    | Generate of int
    | Choose of int

type Page1Model = int list

type Page1VM = {
    Model: Page1Model
    Page2: VmCmd<NavMessages>
} with member x.Count = x.Model.Length
       static member Default = {
                Model = []
                Page2 = Vm.cmd NavMessages.Page2       
       }

let defp1 = Page1VM.Default

let page1Component = 
    Component.create<Page1Model, NavMessages, Messages> [
        <@ defp1.Count @> 
        |> Bind.twoWayValidated 
            (fun vm -> vm.Length) 
            (Validators.greaterThan 10 >> Validators.lessOrEqualTo 2000) 
            Messages.Generate
        <@ defp1.Page2 @> |> Bind.cmd |> Bind.toNav
    ]

type Page2Model = {
    CurrentData : int list
    CurrentGroup : int
    Groups : int []
}

type Page2VM = {
    Model : Page2Model
    Page1: VmCmd<NavMessages>
} with
     static member Default = 
        { Page1 = Vm.cmd NavMessages.Page1 
          Model = 
            { Groups = [| |]
              CurrentData = []
              CurrentGroup = -1 } }

let defp2 = Page2VM.Default

let page2Component = 
    Component.create<Page2Model, NavMessages, Messages> [
        <@ defp2.Model.CurrentData @> |> Bind.oneWay (fun vm -> vm.CurrentData)
        <@ defp2.Model.CurrentGroup @> |> Bind.twoWay (fun vm -> vm.CurrentGroup) Messages.Choose
        <@ defp2.Model.Groups @> |> Bind.oneWay (fun vm -> vm.Groups)
        <@ defp2.Page1 @> |> Bind.cmd |> Bind.toNav
    ]

type Model = { 
    Page1 : Page1Model 
    Page2 : Page2Model
}

let init = {
    Page1 = []
    Page2 = Page2VM.Default.Model
}

open Gjallarhorn.Bindable.Framework

let groups countInGroup data = 
    let count = 
        float (data |> Seq.length) / float countInGroup
        |> Math.Ceiling
        |> int
    [| 1..count |]

let getData data countInGroup current = 
    data
    |> List.skip (countInGroup * (current - 1))
    |> List.truncate countInGroup    

let update message (model : Model) =
    let countInGroup = 50
    match message with
    | Messages.Generate v ->
        let current = 1 
        let data = List.init v id 
        { Page1 = data
          Page2 = {
            CurrentData = getData data countInGroup current
            Groups = groups countInGroup data
            CurrentGroup = current
          }
        }
    | Messages.Choose v -> 
        { model with 
            Page2 = { 
              model.Page2 with 
                CurrentGroup = v 
                CurrentData = getData model.Page1 countInGroup v } }

let appComp =
    Component.create<Model, NavMessages, Messages> [
        <@ init.Page1 @> |> Bind.comp (fun m -> m.Page1) page1Component fst
        <@ init.Page2 @> |> Bind.comp (fun m -> m.Page2) page2Component fst
    ]

let applicationCore nav =
    let navigation = Dispatcher<NavMessages>()
    Framework.application init update appComp nav
    |> Framework.withNavigation navigation

[<EntryPoint>]
[<STAThread>]
let main _ = 
    let updateNavigation (app : ApplicationCore<Model,_,_>) request =
        match request with
        | NavMessages.Page1  ->
            Navigation.Page.fromComponent
                Views.Page1 (fun vm -> vm.Page1) page1Component id
        | NavMessages.Page2 -> 
            Navigation.Page.fromComponent 
                Views.Page2 (fun vm -> vm.Page2) page2Component id

    let navigator = Navigation.singlePage Windows.Application Views.MainWindow NavMessages.Page1 updateNavigation 

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0