<%@ Page Language="C#" MasterPageFile="~/MasterPages/Main.master" %>

<asp:Content ID="Head" runat="server" ContentPlaceHolderID="Head">
    <title>FactionRPG</title>
    <script type="text/javascript" src="/js/perspective.js"></script>
    <script type="text/javascript" src="/js/render.js"></script>
    <script type="text/javascript" src="/js/urlhelper.js"></script>
    <script type="text/javascript">
        $(function () {
            loadGame(getQuery('guid'), getQuery('turn'));
        });
    </script>
</asp:Content>
<asp:Content ID="Body" runat="server" ContentPlaceHolderID="Body">
    <asp:ScriptManager ID="ScriptManager" runat="server">
        <Services>
            <asp:ServiceReference Path="~/JsonServices/GameService.asmx" />
        </Services>
    </asp:ScriptManager>
    <div class="row header-row">
        <h2>
            <%= this.Game.Name %><% if (this.Faction != null)
                                    { %>
            as
            <%= this.Faction.Name%><% } %>
        </h2>
    </div>
    <div class="row">
        <div class="span19">
            <div id="divPreGame" runat="server" visible="false">
                <fieldset>
                    <legend>Facilitate Game</legend>To invite players to join the game, share this link:
                    <asp:TextBox ID="txtShareLink" runat="server" ReadOnly="true" Columns="30" /><br />
                    <br />
                    <asp:ListView ID="lvFactions" runat="server" DataKeyNames="Id" OnItemDeleting="lvFactions_OnItemDeleting">
                        <LayoutTemplate>
                            Manage Players:<br />
                            <ul>
                                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                            </ul>
                        </LayoutTemplate>
                        <ItemTemplate>
                            <li>
                                <%#Eval("PlayerUserName") %>
                                joined as
                                <%#Eval("Name") %>
                                with
                                <%# ((Faction)Container.DataItem).SpentBP() %>
                                BP spent
                                <asp:LinkButton ID="btnRemove" runat="server" Text="Remove" CommandName="Delete" />
                            </li>
                            <%--<% if (this.Game.IsManaged)
                               { %>
                            <asp:ListView ID="lvIntelByFaction" runat="server" DataSource='<%# Service.GetAssetsOfFactionFromPerspective(this.Game, ((Faction)Container.DataItem).Id, this.Game.CurrentTurn, long.Parse(Eval("Id").ToString())).OrderBy(a => a.Name) %>'>
                                <LayoutTemplate>
                                    <ul>
                                        <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                                    </ul>
                                </LayoutTemplate>
                                <ItemTemplate>
                                    <li>
                                        <%# ((Asset)Container.DataItem).Render(this.Faction.Id)%>
                                    </li>
                                </ItemTemplate>
                                <EmptyDataTemplate>
                                    <ul>
                                        <li>(This faction has no assets.)</li>
                                    </ul>
                                </EmptyDataTemplate>
                            </asp:ListView>
                            <%} %>--%>
                        </ItemTemplate>
                        <EmptyDataTemplate>
                            No players have joined this game.<br />
                        </EmptyDataTemplate>
                    </asp:ListView>
                    <asp:Button ID="btnStartGame" runat="server" Text="Start Game" OnClick="btnStartGame_Click" />
                </fieldset>
            </div>
            <div id="divJoinGame" runat="server">
                <fieldset>
                    <legend>Join
                        <%= Game.Name %></legend>
                        TODO: If you are the Facilitator, be able to change all of these fields!
                    <ul>
                        <li>Facilitator:
                            <%= Membership.GetUserNameByEmail(Game.FacilitatorEmail) %></li>
                        <li>Total Number of Turns:
                            <%= Game.TotalTurns %></li>
                        <li>Managed?
                            <%= Game.IsManaged %></li>
                        <li>Turn Length in Minutes:
                            <%= Game.TurnLengthMinutes %></li>
                        <li>Starting Build Points:
                            <%= Game.StartingBuildPoints %></li>
                        <li>Max Initial Asset Value:
                            <%= Game.MaxInitialAssetValue %></li>
                        <li>Create Assets?
                            <%= Game.CreateAssets %></li>
                    </ul>
                    <asp:Button ID="btnJoinGame" runat="server" Text="Join Game" OnClick="btnJoinGame_Click" /></fieldset>
            </div>
            <div id="divCreateFaction" runat="server">
                <fieldset>
                    <legend>Create Faction</legend>You are playing in
                    <%= Game.Name %>
                    as
                    <asp:TextBox ID="txtChangeFactionName" runat="server" /><asp:Button ID="btnChangeFactionName" runat="server" Text="Change Faction Name" OnClick="btnChangeFactionName_Click" /><br />
                    <asp:Label ID="lblHexColour" runat="server" Text="This is your faction's colour code:" /> #<asp:TextBox ID="txtChangeHexColour" runat="server" Width="60" /><asp:Button ID="btnChangeHexColour" runat="server" Text="Change Faction Colour Code" OnClick="btnChangeHexColour_Click" /> <a href="http://www.colorpicker.com/" target="_blank">Find Colour Code</a><br />
                    <asp:Button ID="btnLeaveGame" runat="server" Text="Leave Game" OnClick="btnLeaveGame_Click" /><br />
                    <% if (Faction.SpentBP() < Game.StartingBuildPoints)
                       { %>
                    You must spend your remaining
                    <%= Game.StartingBuildPoints - Faction.SpentBP()%>
                    of
                    <%= Game.StartingBuildPoints%>
                    BP on assets.<br />
                    The maximum initial asset value is
                    <%= Game.MaxInitialAssetValue%>.<br />
                    <asp:DropDownList ID="ddlValue" runat="server" />
                    <asp:TextBox ID="txtName" runat="server" Text="Asset Name" /><br />
                    <asp:TextBox ID="txtImageUrl" runat="server" Text="Image URL" /><br />
                    <asp:CheckBox ID="chkCovert" runat="server" Text="Covert?" /><br />
                    <asp:ListBox ID="lbSpecializations" runat="server" SelectionMode="Multiple" Rows="8" />
                    <br />
                    <asp:Button ID="btnAddAsset" runat="server" Text="Add Asset" OnClick="btnAddAsset_Click" />
                    <asp:Label ID="lblError" runat="server" ForeColor="Red" /><br />
                    <% } %>
                    <asp:ListView ID="lvAssets" runat="server" DataKeyNames="Id" OnItemDeleting="lvAssets_OnItemDeleting">
                        <LayoutTemplate>
                            Your Assets:<br />
                            <ul>
                                <asp:PlaceHolder ID="itemPlaceholder" runat="server" />
                            </ul>
                        </LayoutTemplate>
                        <ItemTemplate>
                            <li>
                                (<%# ((Asset)Container.DataItem).BP() %>BP)
                                <%# ((Asset)Container.DataItem).Render(this.Faction.Id)%>
                                <asp:LinkButton ID="btnRemove" runat="server" Text="Remove" CommandName="Delete" />
                            </li>
                        </ItemTemplate>
                    </asp:ListView>
                </fieldset>
            </div>
            <% if (this.Game.CurrentTurn > 0)
               { %>
            <div id="divPerspective">
            </div>
            <% } %>
        </div>
        <div class="span5">
            <h2>
                <asp:Label ID="lblViewingTurn" runat="server" /></h2>
            <p>
               <%= this.Game.TimeRemainingString()%></p>
               <p><asp:Button ID="btnCommit" runat="server" OnClick="btnCommit_Click" /></p>
               <p><asp:DropDownList ID="ddlTurn" runat="server" AutoPostBack="true" OnSelectedIndexChanged="ddlTurn_SelectedIndexChanged" /></p>
               <p><asp:Button ID="btnCreateScenario" runat="server" Text="Create Scenario" onclick="btnCreateScenario_Click" /></p>
        </div>
    </div>
    <div id="view-profile" title="View Profile">
    </div>
</asp:Content>
<script runat="server" type="text/C#">
    public string MembershipEmail;
    public string MembershipUserName;
    public Game Game;
    public Faction Faction;

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            MembershipEmail = Membership.GetUser().Email;
            MembershipUserName = Membership.GetUser().UserName;
        }
        catch { Response.Redirect(string.Format("~/Account/Login.aspx?ReturnUrl={0}", Request.Url), true); }

        try
        {
            Game = Service.GetGame(Request.QueryString["guid"]);
            Faction = Service.GetFaction(Game.Id, MembershipEmail);

            if (Faction != null && Game.CurrentTurn > 0 && Game.TimeRemaining() < new TimeSpan(0, 0, 0))
            {
                if (Game.TotalTurns == 0 || Game.CurrentTurn <= Game.TotalTurns)
                {
                    Game.ProcessTurn();
                    Response.Redirect(Game.Url());
                }
            }

            if (Faction != null)
            {
                Page.Title = String.Format("{0} - {1} - FactionRPG", Game.Name, Faction.Name);
            }
            else
            {
                Page.Title = String.Format("{0} - FactionRPG", Game.Name);
            }

            if (this.Game.FacilitatorEmail.Equals(this.MembershipEmail))
            {
                if (this.Game.CurrentTurn == 0)
                {
                    divPreGame.Visible = true;
                    btnCommit.Visible = false;
                    btnCreateScenario.Visible = false;

                    txtShareLink.Text = Request.Url.Host + this.Game.Url();

                    lvFactions.DataSource = Service.GetFactionsInGame(this.Game.Id);
                    lvFactions.DataBind();
                }
            }

            if (Game.CurrentTurn == 0)
            {
                if (this.Faction != null || (this.Game.IsManaged && this.Game.FacilitatorEmail.Equals(this.MembershipEmail)))
                {
                    divJoinGame.Visible = false;
                }
            }
            else
            {
                divJoinGame.Visible = false;

                if (this.Faction != null)
                {
                    if (!this.Faction.Committed)
                    {
                        // if not committed
                        btnCommit.Text = "Commit Orders";
                    }
                    else
                    {
                        // if committed, remove commitment
                        btnCommit.Text = "Uncommit Orders";
                    }
                }
                else
                {
                    btnCommit.Visible = false;
                }
            }

            if (this.Faction != null && this.Game.CurrentTurn == 0)
            {
                if (this.Faction != null)
                {
                    if (!IsPostBack)
                    {
                        txtChangeFactionName.Text = Faction.Name;
                        txtChangeHexColour.Text = Faction.HexColour;
                        lblHexColour.Style["background"] = string.Format("#{0}", Faction.HexColour);
                    }

                    lvAssets.DataSource = Service.GetAssetsOfFaction(Faction.Id);
                    lvAssets.DataBind();
                }
                else { divCreateFaction.Visible = false; }

                if (!IsPostBack)
                {
                    for (int i = 1; i <= Game.MaxInitialAssetValue; i++)
                    {
                        ddlValue.Items.Add(new ListItem("Value=" + i, i.ToString()));
                    }

                    lbSpecializations.DataSource = System.Enum.GetValues(typeof(Asset.Specialziations));
                    lbSpecializations.DataBind();
                }
            }
            else
            {
                divCreateFaction.Visible = false;
            }

            int turn;

            if (Game.CurrentTurn > 0)
            {
                if (!int.TryParse(Request.QueryString["turn"], out turn))
                {
                    turn = Game.CurrentTurn - 1;
                }

                if (turn >= Game.CurrentTurn || turn < 1)
                {
                    turn = Game.CurrentTurn - 1;
                }

                for (int i = Game.CurrentTurn - 1; i > 0; i--)
                {
                    ddlTurn.Items.Add(new ListItem("View Turn " + i, i.ToString()));
                }

                if (ddlTurn.Items.Count > 0)
                {
                    lblViewingTurn.Text = "Viewing History for Turn " + turn;
                    ddlTurn.Items.Insert(0, new ListItem("View History", "0"));
                }
                else
                {
                    lblViewingTurn.Text = string.Format("Round {0}", this.Game.CurrentTurn);
                    ddlTurn.Visible = false;
                }
            }
            else
            {
                ddlTurn.Visible = false;
            }
        }
        catch
        {
            //Response.Redirect("~/default.aspx");
            throw;
        }
    }

    protected void lvFactions_OnItemDeleting(object sender, ListViewDeleteEventArgs e)
    {
        try
        {
            if (this.Game.CurrentTurn == 0)
            {
                var factionId = long.Parse(e.Keys["Id"].ToString());

                foreach (var asset in Service.GetAssetsOfFaction(factionId))
                {
                    Service.RemoveAsset(asset.Id);
                }

                Service.RemoveFaction(factionId);
            }

            Response.Redirect(this.Game.Url());
        }
        catch
        {
            e.Cancel = true;
        }
    }

    protected void btnStartGame_Click(object sender, EventArgs e)
    {
        if (this.Game.CurrentTurn == 0)
        {
            // calculate the game's total space
            //int totalSpace = _game.InitialFreeSpace;

            //foreach (var assetId in Service.GetAssetIdsInGame(_game))
            //{
            //    totalSpace += Service.GetAsset(assetId).Value;
            //}

            //Service.SetTotalSpace(_game.Id, totalSpace);

            // actually start the game
            Service.SetGameLastTurnStart(this.Game.Id, DateTime.Now);
            foreach (var assetId in Service.GetAssetIdsInGame(this.Game))
            {
                Service.SetDefaultAction(assetId);
            }
            this.Game.ProcessTurn();
        }

        Response.Redirect(this.Game.Url());
    }

    protected void btnJoinGame_Click(object sender, EventArgs e)
    {
        var game = Service.GetGame(Request.QueryString["guid"]);

        if (game.CurrentTurn == 0)
        {
            var faction = new Faction
            {
                Name = this.MembershipUserName + "'s Faction",
                PlayerEmail = this.MembershipEmail,
                PlayerUserName = this.MembershipUserName
            };

            Service.AddFactionToGame(game.Id, faction);
        }

        Response.Redirect(game.Url());
    }

    protected void btnLeaveGame_Click(object sender, EventArgs e)
    {
        if (Game.CurrentTurn == 0)
        {
            foreach (var asset in Service.GetAssetsOfFaction(this.Faction.Id))
            {
                Service.RemoveAsset(asset.Id);
            }

            Service.RemoveFaction(this.Faction.Id);
        }

        Response.Redirect(Game.Url());
    }

    protected void btnChangeFactionName_Click(object sender, EventArgs e)
    {
        if (this.Game.CurrentTurn == 0)
        {
            Service.SetFactionName(this.Faction.Id, Microsoft.Security.Application.AntiXss.HtmlEncode(txtChangeFactionName.Text));
        }

        Response.Redirect(Game.Url());
    }

    protected void btnChangeHexColour_Click(object sender, EventArgs e)
    {
        if (this.Game.CurrentTurn == 0)
        {
            Service.SetFactionHexColour(this.Faction.Id, Microsoft.Security.Application.AntiXss.HtmlEncode(txtChangeHexColour.Text));
        }

        Response.Redirect(Game.Url());
    }

    protected void btnAddAsset_Click(object sender, EventArgs e)
    {
        if (this.Game.CurrentTurn == 0)
        {
            try
            {
                var assetName = Microsoft.Security.Application.AntiXss.HtmlEncode(txtName.Text);
                var assetImageUrl = Microsoft.Security.Application.AntiXss.HtmlEncode(txtImageUrl.Text);

                var asset = new Asset()
                {
                    Turn = 0,
                    Value = int.Parse(ddlValue.SelectedValue),
                    Name = assetName,
                    ImageUrl = assetImageUrl,
                    Covert = chkCovert.Checked,
                    HasAmbush = lbSpecializations.Items[(int)Asset.Specialziations.Ambush].Selected,
                    HasSalvage = lbSpecializations.Items[(int)Asset.Specialziations.Salvage].Selected,
                    HasDisguise = lbSpecializations.Items[(int)Asset.Specialziations.Disguise].Selected,
                    HasVanish = lbSpecializations.Items[(int)Asset.Specialziations.Vanish].Selected,
                    HasManeuver = lbSpecializations.Items[(int)Asset.Specialziations.Maneuver].Selected,
                    HasInfuse = lbSpecializations.Items[(int)Asset.Specialziations.Infuse].Selected,
                    HasInvestigate = lbSpecializations.Items[(int)Asset.Specialziations.Investigate].Selected,
                    HasPropagate = lbSpecializations.Items[(int)Asset.Specialziations.Propagate].Selected,
                };

                if (asset.BP() + Faction.SpentBP() > Game.StartingBuildPoints)
                {
                    throw new Exception("Insufficient BP remaining.");
                }

                if (assetName.Length < 4)
                {
                    throw new Exception("Asset names must be at least 4 characters long.");
                }

                if (new Regex(@"[0-9]", RegexOptions.IgnoreCase).Match(assetName).Success)
                {
                    throw new Exception("Asset names cannot include numbers.");
                }

                if (Service.GetAssetsWithSameNameInGame(Game, asset).Count() > 0)
                {
                    throw new Exception("Asset names must be unique.");
                }

                Service.AddAssetToFaction(Faction.Id, asset);

                // factions need to get profiles on their own assets
                if (asset.Covert)
                {
                    Service.AddProfileOnAssetToFaction(Faction.Id, asset);
                }

                //asset.ComputeResult();

                Response.Redirect(Game.Url());
            }
            catch (Exception ex)
            {
                lblError.Text = ex.Message;
            }
        }
        else
        {
            Response.Redirect(Game.Url());
        }
    }

    protected void lvAssets_OnItemDeleting(object sender, ListViewDeleteEventArgs e)
    {
        if (this.Game.CurrentTurn == 0)
        {
            try
            {
                Service.RemoveAsset(long.Parse(e.Keys["Id"].ToString()));

                Response.Redirect(Game.Url());
            }
            catch
            {
                e.Cancel = true;
            }
        }
        else
        {
            Response.Redirect(Game.Url());
        }
    }

    protected void btnCommit_Click(object sender, EventArgs e)
    {
        // ensure that the request is not being sent after the turn has already been processed
        if (this.Game.CurrentTurn == Service.GetGame(Request.QueryString["guid"]).CurrentTurn)
        {
            if (Service.GetFaction(this.Faction.Id).Committed)
            {
                // if committed, un-commit
                Service.SetCommitment(this.Faction.Id, false);
            }
            else
            {
                // if not committed, commit
                Service.SetCommitment(this.Faction.Id, true);

                // if every faction in the game has committed, process turn
                var everyoneCommitted = true;

                foreach (var faction in Service.GetFactionsInGame(this.Game.Id))
                {
                    if (!faction.Committed)
                    {
                        everyoneCommitted = false;
                        break;
                    }
                }

                if (everyoneCommitted)
                {
                    this.Game.ProcessTurn();
                }
            }
        }

        Response.Redirect(this.Game.Url());
    }

    protected void ddlTurn_SelectedIndexChanged(object sender, EventArgs e)
    {
        Response.Redirect(string.Format("{0}&turn={1}", this.Game.Url(), ddlTurn.SelectedValue));
    }

    protected void btnCreateScenario_Click(object sender, EventArgs e)
    {
        Response.Redirect(this.Game.CreateScenario(this.Game.CurrentTurn, this.MembershipEmail).Url());
    }
</script>
