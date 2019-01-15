open System

[<RequireQualifiedAccess>]
module Views = 
    open FsXaml
    open System.Windows.Controls
    open System.Windows

    type MainWindow = XAML<"MainWindow.xaml">
    type Page1 = XAML<"Page1.xaml">
    type Menu = XAML<"Menu.xaml">
    type InitView= XAML<"InitView.xaml">

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
    let createInit = InitView >> create

open Gjallarhorn.Wpf
open Gjallarhorn.Bindable

[<RequireQualifiedAccess>]
type NavMessages = 
    | Init
    | Page1
    | Page2

[<RequireQualifiedAccess>]
type MenuMessages = 
    | ToPage1
    | ToPage2
    | GoOut
    | ToStart
    
[<RequireQualifiedAccess>]
type Messages = 
    | MenuUpdate of MenuMessages
    | GoIn

[<RequireQualifiedAccess>]
type State = 
    | In
    | Out

type PageVM = { Text : string } 

let def = { Text = "" }

let pageComponent = 
    Component.create<PageVM, NavMessages, Messages> [
        <@ def.Text @>  |> Bind.oneWay (fun v -> v.Text)
    ]

type InitVM = { Message : VmCmd<Messages> } 

let defi = { Message = Vm.cmd Messages.GoIn }

let initComponent = 
    Component.create<InitVM, NavMessages, Messages> [
        <@ defi.Message @>  |> Bind.cmd
    ]

type MenuItem = { 
    IsEnabled : bool
    Text : string
    Command : MenuMessages
} 

type Menu = {
    Items : MenuItem list
    State : State
}

type MenuVM = {
    Menu : Menu
    Request : VmCmd<MenuMessages>
}

let createMenuItem state =
    let toMenuItem (title, command, state) = 
        { Text = title
          Command = command
          IsEnabled = state }

    match state with
    | State.In ->
        [ "Page1", MenuMessages.ToPage1, true
          "Page2", MenuMessages.ToPage2, true
          "GoOut", MenuMessages.GoOut, true ]
    | State.Out ->
        [ "Page1", MenuMessages.ToPage1, true
          "Page2", MenuMessages.ToPage2, false
          "ToInit", MenuMessages.ToStart, true ]
    |> List.map toMenuItem

let createMenu state = 
    { Items = state |> createMenuItem
      State = state }
    
let defMenu = { 
    Menu = 
      { Items = []
        State = State.Out }
    Request = Vm.cmd MenuMessages.ToStart
}

let menuComponent = 
    Component.create<Menu, NavMessages, MenuMessages> [
        <@ defMenu.Menu.Items @> |> Bind.oneWay (fun vm -> vm.Items)
        <@ defMenu.Request @> |> Bind.cmdParam (fun vm -> vm)
        <@ defMenu.Menu.State @> |> Bind.oneWay (fun vm -> vm.State)
    ] |> Component.withMappedMessages Messages.MenuUpdate

let toNavigation =
    function
    | MenuMessages.ToPage1 -> NavMessages.Page1
    | MenuMessages.ToPage2 -> NavMessages.Page2
    | MenuMessages.ToStart 
    | MenuMessages.GoOut -> NavMessages.Init

let upd (nav : Dispatcher<NavMessages>) message model = 
    printfn "mesage = %A" message
    match message with
    | Messages.MenuUpdate upd -> 
        upd |> toNavigation |> nav.Dispatch
        match upd with
        | MenuMessages.GoOut -> State.Out
        | _ -> model.State 
        |> createMenu
    | Messages.GoIn ->
        NavMessages.Page1 |> nav.Dispatch
        State.In |> createMenu

open Gjallarhorn.Bindable.Framework

let applicationCore nav =
    let navigation = Dispatcher<NavMessages>()
    let state = State.Out
    let init = createMenu state
    Framework.application init (upd navigation) menuComponent nav
    |> Framework.withNavigation navigation

[<EntryPoint>]
[<STAThread>]
let main _ = 
    let updateNavigation (app : ApplicationCore<Menu,_,_>) request =
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
        | NavMessages.Init  ->
            Navigation.Page.fromComponent
                Views.createInit
                (fun _ -> defi)
                initComponent 
                id

    let navigator = 
        Navigation.singlePage 
            Windows.Application 
            Views.MainWindow 
            NavMessages.Init
            updateNavigation 

    let app = applicationCore navigator.Navigate

    Framework.RunApplication (navigator, app)
    0