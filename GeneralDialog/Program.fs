open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml
    open System.Windows.Controls

    type MainWindow = XAML<"MainWindow.xaml">
    type FieldView = XAML<"FieldView.xaml">

    type ValueView = XAML<"ValueView.xaml">
    type AddDialogBase = XAML<"AddDialog.xaml">

    type AddDialog(content:UserControl, title) as self =
        inherit AddDialogBase()

        do self.wnd.Title <- title
        do self.content.Content <- content

        override this.CloseClick (_sender, _e) = this.Close()

    let valueAddDialog() = AddDialog(ValueView(), "AddValue")
    let valueSubtractDialog() = AddDialog(ValueView(), "SubtractValue")

open Gjallarhorn.Wpf
open Gjallarhorn.Bindable

[<RequireQualifiedAccess>]
type NavMessages = 
    | MainPage
    | AddRequest
    | SubtractRequest

[<RequireQualifiedAccess>]
type FieldMessages = 
    | Add of int
    | Subtract of int

type FieldVM = {
    Value : int
    Add: VmCmd<NavMessages>
    Subtract: VmCmd<NavMessages>
}

let def = {
    Value = 0
    Add = Vm.cmd NavMessages.AddRequest
    Subtract = Vm.cmd NavMessages.SubtractRequest
}

let fieldComponent = 
    Component.create<FieldVM, NavMessages, FieldMessages> [
        <@ def.Value @> |> Bind.oneWay (fun vm -> vm.Value)
        <@ def.Add @> |> Bind.cmd |> Bind.toNav
        <@ def.Subtract @> |> Bind.cmd |> Bind.toNav
    ]

module GeneralComponent = 
    open Gjallarhorn
    let addExplicit c nav (source:BindingSource) model =
        let v = model |> Signal.get |> Mutable.create
        Bind.Explicit.componentOneWay source "Comp" nav c v
        |> Observable.subscribe (fun t -> v.Value <- t)
        |> source.AddDisposable

        [
            Bind.Explicit.createCommandChecked "SaveCommand" source.Valid source
            |> Observable.map (fun _ -> v.Value)
        ]

    let addComponent c = addExplicit c |> Component.fromExplicit

module Components = 
    open Gjallarhorn.Validation

    type ValueVM = { Value : int }

    [<RequireQualifiedAccess>]
    type ValueMessages = | Update of int

    let defv = { Value = 0 }

    let upd message _ = 
        match message with
        | ValueMessages.Update v -> { Value = v }
    let valueComponent = 
        Component.create<ValueVM, NavMessages, ValueMessages> [
            <@ defv.Value @> |> Bind.twoWayValidated (fun vm -> vm.Value) (Validators.greaterThan 0) ValueMessages.Update
        ] |> Component.toSelfUpdating upd 
        |> GeneralComponent.addComponent
        |> Component.withMappedMessages (fun vm -> vm.Value)

open Gjallarhorn.Bindable.Framework

let update message model =
    match message with
    | FieldMessages.Add v -> 
        { model with Value = model.Value + v }
    | FieldMessages.Subtract v -> 
        { model with Value = model.Value - v }   
        
let applicationCore nav =
    let navigation = Dispatcher<NavMessages>()
    Framework.application def update fieldComponent nav
    |> Framework.withNavigation navigation

[<EntryPoint>]
[<STAThread>]
let main argv = 
    let updateNavigation (app : ApplicationCore<FieldVM,_,_>) request =
        match request with
        | NavMessages.MainPage  ->
            Navigation.Page.create Views.FieldView fieldComponent
        | NavMessages.AddRequest -> 
            Navigation.Page.dialog 
                Views.valueAddDialog (fun _ -> Components.defv) Components.valueComponent FieldMessages.Add
        | NavMessages.SubtractRequest -> 
            Navigation.Page.dialog 
                Views.valueSubtractDialog (fun _ -> Components.defv) Components.valueComponent FieldMessages.Subtract

    let navigator = Navigation.singlePage Windows.Application Views.MainWindow NavMessages.MainPage updateNavigation 

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0
