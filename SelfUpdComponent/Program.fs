open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml

    type MainWindow = XAML<"MainWindow.xaml">
    type Page1 = XAML<"Page1.xaml">

    type Page2 = XAML<"Page2.xaml">

open Gjallarhorn.Wpf
open Gjallarhorn.Bindable
open Gjallarhorn
open Gjallarhorn.Validation

module WrapperComponent = 
    let private explicit name c nav (source:BindingSource) model =
        let v = model |> Signal.get |> Mutable.create
        Bind.Explicit.componentOneWay source name nav c v
        |> Observable.subscribe (fun t -> v.Value <- t)
        |> source.AddDisposable

        [ ]

    let bindIgnoreMessageComponentWithName name c = 
        explicit name c |> Component.fromExplicit

[<RequireQualifiedAccess>]
type NavMessages = 
    | Page1
    | Page2

type Model = int list

[<RequireQualifiedAccess>]
type Page1Messages = 
    | Generate of int
    | Nothing 

type Page1VM = {
    Value: Model
    Page2: VmCmd<NavMessages>
} with member x.Count = x.Value.Length

let defp1 = {
    Value = []
    Page2 = Vm.cmd NavMessages.Page2
}

let page1Component = 
    Component.create<Page1VM, NavMessages, Page1Messages> [
        <@ defp1.Count @> 
        |> Bind.twoWayValidated 
            (fun vm -> vm.Count) 
            (Validators.greaterThan 10 >> Validators.lessOrEqualTo 2000) 
            Page1Messages.Generate
        <@ defp1.Page2 @> |> Bind.cmd |> Bind.toNav
    ]

[<RequireQualifiedAccess>]
type Page2Messages = 
    | ChooseGroup of int

type Page2VM = {
    Data : Model
    CurrentGroup : int
    CountInGroup : int
    Page1: VmCmd<NavMessages>
} 
with member x.CurrentData = 
        x.Data
        |> List.skip (x.CountInGroup * (x.CurrentGroup - 1))
        |> List.truncate x.CountInGroup
     member x.Groups = 
        let count = 
            float x.Data.Length / float x.CountInGroup
            |> Math.Ceiling
            |> int
        [| 1..count |]
     static member Create data = 
        let current = 1
        let initCount = 50
        { Page1 = Vm.cmd NavMessages.Page1 
          Data = data
          CurrentGroup = current
          CountInGroup = initCount }

let defp2 = {
    Data = []
    CurrentGroup = 0
    CountInGroup = 0
    Page1 = Vm.cmd NavMessages.Page1
} 
let upd message model = 
    match message with
    | Page2Messages.ChooseGroup v -> { model with CurrentGroup = v }

let page2Component : IComponent<Page2VM, NavMessages, Page1Messages> = 
    Component.create<Page2VM, NavMessages, Page2Messages> [
        <@ defp2.CurrentData @> |> Bind.oneWay (fun vm -> vm.CurrentData)
        <@ defp2.CurrentGroup @> |> Bind.twoWay (fun vm -> vm.CurrentGroup) Page2Messages.ChooseGroup
        <@ defp2.Groups @> |> Bind.oneWay (fun vm -> vm.Groups)
        <@ defp2.Page1 @> |> Bind.cmd |> Bind.toNav
    ] |> Component.toSelfUpdating upd
    |> WrapperComponent.bindIgnoreMessageComponentWithName "Comp"

open Gjallarhorn.Bindable.Framework
let update message model =
    match message with
    | Page1Messages.Generate v -> 
        { model with Value = List.init v id }
    | Page1Messages.Nothing -> model

let applicationCore nav =
    let navigation = Dispatcher<NavMessages>()
    Framework.application defp1 update page1Component nav
    |> Framework.withNavigation navigation

[<EntryPoint>]
[<STAThread>]
let main _ = 
    let updateNavigation (app : ApplicationCore<Page1VM,_,_>) request =
        match request with
        | NavMessages.Page1  ->
            Navigation.Page.create Views.Page1 page1Component
        | NavMessages.Page2 -> 
            Navigation.Page.fromComponent 
                Views.Page2
                (fun p1 -> p1.Value |> Page2VM.Create) 
                page2Component 
                (fun _ -> Page1Messages.Nothing)

    let navigator = Navigation.singlePage Windows.Application Views.MainWindow NavMessages.Page1 updateNavigation 

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0
