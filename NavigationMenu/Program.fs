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

module PageComponent = 
    type PageModel = { PageText : string } 

    let def = { PageText = "" }

    let pageComponent = 
        Component.create<PageModel, NavMessages, Messages> [
            <@ def.PageText @>  |> Bind.oneWay (fun v -> v.PageText)
        ]

module InitComponent = 
    type InitModel = { Text : string }
    type InitVM = { Model: InitModel; Message : VmCmd<Messages> } 
    
    let defi = { Model = { Text = "" } ; Message = Vm.cmd Messages.GoIn }

    let initComponent = 
        Component.create<InitModel, NavMessages, Messages> [
            <@ defi.Message @>  |> Bind.cmd
            <@ defi.Model.Text @> |> Bind.oneWay (fun v -> v.Text)
        ]


[<RequireQualifiedAccess>]
type State = 
     | In
     | Out

module MenuComponent = 
    type MenuItem = { 
        IsEnabled : bool
        Text : string
        Command : MenuMessages
    } 

    type Menu = { Items : MenuItem list }

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

    let createMenu state = { Items = state |> createMenuItem }
    
    let defMenu = { 
        Menu = { Items = []  }
        Request = Vm.cmd MenuMessages.ToStart
    }

    let toNavigation =
        function
        | MenuMessages.ToPage1 -> NavMessages.Page1
        | MenuMessages.ToPage2 -> NavMessages.Page2
        | MenuMessages.ToStart 
        | MenuMessages.GoOut -> NavMessages.Init

    let menuComponent = 
        Component.create<Menu, NavMessages, MenuMessages> [
            <@ defMenu.Menu.Items @> |> Bind.oneWay (fun vm -> vm.Items)
            <@ defMenu.Request @> |> Bind.cmdParam (fun vm -> vm)
        ] |> Component.withMappedMessages Messages.MenuUpdate

open MenuComponent
open PageComponent
open InitComponent

// App
type AppModel = { 
    Page : PageModel 
    Menu : Menu
    Init : InitModel
    State : State
}

let init = {
    Page = { PageText = "" }
    Menu = createMenu State.Out
    Init = { Text = "Some text" }
    State = State.Out
}

let appComp =
    Component.create<AppModel, NavMessages, Messages> [
        <@ init.Page @> |> Bind.comp (fun m -> m.Page) pageComponent fst
        <@ init.Menu @> |> Bind.comp (fun m -> m.Menu) menuComponent fst
        <@ init.Init @> |> Bind.comp (fun m -> m.Init) initComponent fst
    ]

let upd (nav : Dispatcher<NavMessages>) message model = 
    printfn "mesage = %A" message
    match message with
    | Messages.MenuUpdate upd -> 
        upd |> toNavigation |> nav.Dispatch
        match upd with
        | MenuMessages.GoOut -> 
            let state = State.Out
            { model with State = state; Menu = state |> createMenu }
        | _ -> model
    | Messages.GoIn ->
        NavMessages.Page1 |> nav.Dispatch
        let state = State.In
        { model with Menu = state |> createMenu ; State = state }

open Gjallarhorn.Bindable.Framework

let applicationCore nav =
    let navigation = Dispatcher<NavMessages>()
    Framework.application init (upd navigation) appComp nav
    |> Framework.withNavigation navigation

[<EntryPoint>]
[<STAThread>]
let main _ = 
    let updateNavigation (app : ApplicationCore<AppModel,_,_>) request =
        match request with
        | NavMessages.Page1  ->
            Navigation.Page.fromComponent
                Views.createPage1
                (fun _ -> { PageText = "This is the first page" })
                pageComponent 
                id
        | NavMessages.Page2  ->
            Navigation.Page.fromComponent
                Views.createPage2
                (fun _ -> { PageText = "This is the second page" })
                pageComponent 
                id
        | NavMessages.Init  ->
            Navigation.Page.fromComponent
                Views.createInit
                (fun m -> m.Init)
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