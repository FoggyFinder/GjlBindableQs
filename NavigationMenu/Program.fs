open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml
    open System.Windows.Controls
    open System.Windows

    type MainWindow = XAML<"MainWindow.xaml">
    type Page1 = XAML<"Page1.xaml">
    type Menu = XAML<"Menu.xaml">
    
    let create (fe : FrameworkElement) =
          let dock = DockPanel(LastChildFill=true)
          DockPanel.SetDock(fe, Dock.Left)

          Menu() |> dock.Children.Add |> ignore
          dock.Children.Add fe |> ignore

          dock :> FrameworkElement

    let createPage1 = Page1 >> create
    let createPage2 = Page1 >> create   
    let createPage3 = Page1 >> create
    let createPage4 = Page1 >> create 
    
open Gjallarhorn.Wpf
open Gjallarhorn.Bindable

[<RequireQualifiedAccess>]
type NavMessages = 
    | Page1
    | Page2
    | Page3
    | Page4

[<RequireQualifiedAccess>]
type Messages = 
    | Page1Request
    | Page2Request
    | Page3Request
    | Page4Request

type Model = bool

type PageVM = {
    Text : string
} 
let def = { Text = "" }
let pageComponent = 
    Component.create<PageVM, NavMessages, Messages> [
        <@ def.Text @>  |> Bind.oneWay (fun v -> v.Text)
    ]

type MenuItem = { 
    IsEnabled : bool
    Text : string
    Command : Messages
} 

type MenuVM = {
    Items : MenuItem list
    Request : VmCmd<Messages>
}

let updateMenu states menu = 
    {
        Items = List.map2 (fun item state -> { item with IsEnabled = state }) menu.Items states
        Request = menu.Request
    }

let upd (nav : Dispatcher<NavMessages>) message model = 
    printfn "mesage = %A" message
    match message with
    | Messages.Page1Request -> 
        NavMessages.Page1 |> nav.Dispatch
        model |> updateMenu [ true; true; false; true ]
    | Messages.Page2Request -> 
        NavMessages.Page2 |> nav.Dispatch
        model |> updateMenu [ true; true; false; false ]
    | Messages.Page3Request -> 
        NavMessages.Page3 |> nav.Dispatch
        model |> updateMenu [ false; true; false; false ]
    | Messages.Page4Request -> 
        NavMessages.Page4 |> nav.Dispatch
        model |> updateMenu [ true; true; true; true ]

let defMenu = { Items = []; Request = Vm.cmd Messages.Page1Request }

let menuComponent = 
    Component.create<MenuVM, NavMessages, Messages> [
        <@ defMenu.Items @> |> Bind.oneWay (fun vm -> vm.Items)
        <@ defMenu.Request @> |> Bind.cmdParam (fun vm -> vm)
    ]

open Gjallarhorn.Bindable.Framework

let create state = 
    let commands = [
        "Page1", Messages.Page1Request
        "Page2", Messages.Page2Request
        "Page3", Messages.Page3Request
        "Page4", Messages.Page4Request
    ]
    commands
    |> List.map (fun (title, vm) -> {
        IsEnabled = state
        Command = vm
        Text = title
    })
let applicationCore nav =
    let navigation = Dispatcher<NavMessages>()

    let state = true
    let init = { 
        Items = create state; 
        Request = Vm.cmd Messages.Page1Request 
    }
    Framework.application init (upd navigation) menuComponent nav
    |> Framework.withNavigation navigation

[<EntryPoint>]
[<STAThread>]
let main _ = 
    let updateNavigation (app : ApplicationCore<MenuVM,_,_>) request =
        match request with
        | NavMessages.Page1  ->
            Navigation.Page.fromComponent 
                Views.createPage1
                (fun _ -> { Text = "This is the first page" }) 
                pageComponent 
                id
        | NavMessages.Page2  ->
            Navigation.Page.fromComponent 
                Views.createPage2
                (fun _ -> { Text = "This is the second page" }) 
                pageComponent 
                id
        | NavMessages.Page3  ->
            Navigation.Page.fromComponent 
                Views.createPage3
                (fun _ -> { Text = "This is the third page" }) 
                pageComponent 
                id
        | NavMessages.Page4  ->
            Navigation.Page.fromComponent 
                Views.createPage4
                (fun _ -> { Text = "This is the fourths page" }) 
                pageComponent 
                id

    let navigator = 
        Navigation.singlePage 
            Windows.Application 
            Views.MainWindow 
            NavMessages.Page1 
            updateNavigation 

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0
