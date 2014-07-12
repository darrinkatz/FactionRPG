<%@ Page Title="" Language="C#" MasterPageFile="~/MasterPages/Main.master" %>

<asp:Content ID="Head" ContentPlaceHolderID="Head" Runat="Server" >
</asp:Content>
<asp:Content ID="Body" ContentPlaceHolderID="Body" Runat="Server">
    <div class="row">
    <div class="span12">
    <asp:ListView ID="lvGamesFacilitated" runat="server">
        <LayoutTemplate>
            Games You Are Facilitating:<br />
            <ul>
                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
            </ul>
        </LayoutTemplate>
        <ItemTemplate>
            <li><a href="/game.aspx?guid=<%# Eval("Guid") %>">
                <%# Eval("Name") %></a> (<%# ((Game)Container.DataItem).TimeRemainingString() %>)</li>
        </ItemTemplate>
        <EmptyDataTemplate>
            You are not facilitating any games.</EmptyDataTemplate>
    </asp:ListView>
    <br />
    <asp:ListView ID="lvGamesPlaying" runat="server">
        <LayoutTemplate>
            Games You Are Playing:<br />
            <ul>
                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
            </ul>
        </LayoutTemplate>
        <ItemTemplate>
            <li><a href="/game.aspx?guid=<%# Eval("Guid") %>">
                <%# Eval("Name") %></a> as
                <%# Service.GetFactionsInGame(long.Parse(Eval("Id").ToString())).FirstOrDefault(f => f.PlayerEmail.Equals(MembershipEmail)).Name %>
                (<%# ((Game)Container.DataItem).TimeRemainingString() %>) </li>
        </ItemTemplate>
        <EmptyDataTemplate>
            You are not playing any games.</EmptyDataTemplate>
    </asp:ListView>
    <br />
    <asp:ListView ID="lvGamesComplete" runat="server">
        <LayoutTemplate>
            Games You Have Completed:<br />
            <ul>
                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
            </ul>
        </LayoutTemplate>
        <ItemTemplate>
            <li><a href="/game.aspx?guid=<%# Eval("Guid") %>">
                <%# Eval("Name") %></a> as
                <%# Service.GetFactionsInGame(long.Parse(Eval("Id").ToString())).FirstOrDefault(f => f.PlayerEmail.Equals(MembershipEmail)).Name %></li>
        </ItemTemplate>
        <EmptyDataTemplate>
            You have not completed any games.</EmptyDataTemplate>
    </asp:ListView>
    </div>
    <div class="span12">
    <fieldset>
        <legend>Start Scenario</legend>
        TODO: scoll window with descriptions<br />
        <asp:DropDownList ID="ddlStartScenario" runat="server" onselectedindexchanged="ddlStartScenario_SelectedIndexChanged" AutoPostBack="true" DataTextField="Name" DataValueField="Id" />
    </fieldset>
    </div>
    <div class="span12">
    <fieldset>
        <legend>Create New Game</legend>Game Name:
        <asp:TextBox ID="txtGameName" runat="server" Text="Game Name" /><br />
        Total Number of Turns:
        <asp:TextBox ID="txtTotalTurns" runat="server" Columns="2" Text="0" />
        (Set to 0 for unlimited turns.)<br />
        Turn Length:
        <asp:DropDownList ID="ddlTurnLength" runat="server">
            <asp:ListItem Text="15 minutes" Value="15" />
            <asp:ListItem Text="30 minutes" Value="30" />
            <asp:ListItem Text="1 hour" Value="60" />
            <asp:ListItem Text="12 hours" Value="720" />
            <asp:ListItem Text="1 day" Value="1440" />
            <asp:ListItem Text="2 days" Value="2880" />
            <asp:ListItem Text="4 days" Value="5760" />
            <asp:ListItem Text="1 week" Value="10080" />
            <asp:ListItem Text="2 weeks" Value="20160" />
        </asp:DropDownList>
        <br />
        Build Points:
        <asp:TextBox ID="txtStartingBuildPoints" runat="server" Columns="2" Text="12" />
        (Max: 100)<br />
        Max Initial Asset Value:
        <asp:TextBox ID="txtMaxInitialAssetValue" runat="server" Columns="2" Text="4" />
        (Max: 100)<br />
        <%--    Free Space:
    <asp:TextBox ID="txtFreeSpace" runat="server" Columns="2" Text="0" /> (Max: 100)<br />
        --%>
        <asp:CheckBox ID="chkCreateAssets" runat="server" Text="Factions are allowed to create new assets during the game." Checked="true" /><br />
        <asp:CheckBox ID="chkManaged" runat="server" Text="Managed Game? (The facilitator cannot play in a managed game.)" Checked="false" /><br />
        <br />
        <asp:Button ID="btnCreateGame" runat="server" Text="Create Game" OnClick="btnCreateGame_Click" />
        <asp:Label ID="lblError" runat="server" ForeColor="Red" />
    </fieldset>
    </div>
    </div>
</asp:Content>
<script runat="server" type="text/C#">
public string MembershipEmail;

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            MembershipEmail = Membership.GetUser().Email;
        }
        catch
        {
            Response.Redirect(string.Format("~/Account/Login.aspx?ReturnUrl={0}", Request.Url), true);
        }

        lvGamesFacilitated.DataSource = Service.GetGamesFacilitatedByUser(MembershipEmail);
        lvGamesFacilitated.DataBind();

        lvGamesPlaying.DataSource = Service.GetGamesPlayedByUser(MembershipEmail);
        lvGamesPlaying.DataBind();

        lvGamesComplete.DataSource = Service.GetGamesCompletedByUser(MembershipEmail);
        lvGamesComplete.DataBind();

        ddlStartScenario.DataSource = Service.GetAllScenarios();
        ddlStartScenario.DataBind();
    }

    protected void btnCreateGame_Click(object sender, EventArgs e)
    {
        try
        {
            if (txtGameName.Text.Length < 1 || txtGameName.Text.Length > 100)
            {
                throw new Exception("Game Name must be between 1 and 100 characters.");
            }

            if (int.Parse(txtTotalTurns.Text) < 0 || int.Parse(txtTotalTurns.Text) > 100)
            {
                throw new Exception("txtTotalTurns must be between 0 and 100.");
            }

            if (TimeSpan.Parse(ddlTurnLength.SelectedValue) <= new TimeSpan(0, 0, 0))
            {
                throw new Exception("You must select a Turn Length.");
            }

            if (int.Parse(txtStartingBuildPoints.Text) < 1 || int.Parse(txtStartingBuildPoints.Text) > 100)
            {
                throw new Exception("Build Points must be between 1 and 100.");
            }

            if (int.Parse(txtMaxInitialAssetValue.Text) < 1 || int.Parse(txtMaxInitialAssetValue.Text) > 100)
            {
                throw new Exception("Max Initial Asset Value must be between 1 and 100.");
            }

            //if (int.Parse(txtFreeSpace.Text) < -100 || int.Parse(txtFreeSpace.Text) > 100)
            //{
            //    throw new Exception("Free Space must be between -100 and 100.");
            //}

            var game = new Game
            {
                FacilitatorEmail = MembershipEmail,
                Name = Microsoft.Security.Application.AntiXss.HtmlEncode(txtGameName.Text),
                TotalTurns = int.Parse(txtTotalTurns.Text),
                IsManaged = chkManaged.Checked,
                CurrentTurn = 0,
                LastTurnStart = DateTime.Now,
                TurnLengthMinutes = int.Parse(ddlTurnLength.SelectedValue),
                StartingBuildPoints = int.Parse(txtStartingBuildPoints.Text),
                MaxInitialAssetValue = int.Parse(txtMaxInitialAssetValue.Text),
                CreateAssets = chkCreateAssets.Checked,
                //InitialFreeSpace = int.Parse(txtFreeSpace.Text),
            };

            Service.CreateGame(game);

            Response.Redirect(game.Url());
        }
        catch (Exception ex)
        {
            lblError.Text = ex.Message;
        }
    }

    protected void ddlStartScenario_SelectedIndexChanged(object sender, EventArgs e)
    {

    }
</script>