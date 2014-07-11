require "spec_helper"

feature "create faction:" do

  scenario "sees faction list" do
    visit factions_path
    expect(page).to have_content "factions"
  end

  scenario "user creates faction" do
    user_creates_a_faction "House Dimir", "darrinkatz@gmail.com"

    expect(page).to have_content "House Dimir"
    expect(page).to have_content "darrinkatz@gmail.com"

    user_creates_asset_from_faction_page "Szadek", 5, covert: true

    expect(page).to have_content "House Dimir"
    expect(page).to have_content "5-Szadek (covert)"
  end

  scenario "user starts first turn, sets orders, and processes" do
    user_creates_a_faction "House Dimir", "dimir@example.com"
    user_creates_asset_from_faction_page "Szadek", 5, covert: true
    user_creates_asset_from_faction_page "Duskmantle", 4, covert: true
    user_creates_asset_from_faction_page "Necromancers", 2, covert: true

    user_creates_a_faction "Selesnya Conclave", "selesnya@example.com"
    user_creates_asset_from_faction_page "Mat'Selesnya", 5, covert: false
    user_creates_asset_from_faction_page "Vitu-Ghazi", 3, covert: false
    user_creates_asset_from_faction_page "Quietmen", 3, covert: false

    visit factions_path
    click_on "Start Turn #1"
    click_on "Set orders"

    user_sets_order_for_asset "5-Szadek", "Attack", "5-Mat'Selesnya"
    user_sets_order_for_asset "4-Duskmantle", "Attack", "3-Vitu-Ghazi"
    user_sets_order_for_asset "2-Necromancers", "Attack", "3-Quietmen"

    user_sets_order_for_asset "5-Mat'Selesnya", "Attack", "5-Szadek"
    user_sets_order_for_asset "3-Vitu-Ghazi", "Attack", "4-Duskmantle"
    user_sets_order_for_asset "3-Quietmen", "Attack", "2-Necromancers"

    click_on "Save orders"
    click_on "Process Turn #1"

    page.should have_content "Szadek attacked Mat'Selesnya: Failure!"
    page.should have_content "Duskmantle attacked Vitu-Ghazi: Success!"
    page.should have_content "Necromancers attacked Quietmen: Failure!"
    page.should have_content "Mat'Selesnya attacked Szadek: Failure!"
    page.should have_content "Vitu-Ghazi attacked Duskmantle: Failure!"
    page.should have_content "Quietmen attacked Necromancers: Success!"
  end

end

def user_creates_a_faction name, email
    visit factions_path
    click_on "create faction"

    expect(page).to have_content "create a faction"

    fill_in "Faction name", with: name
    fill_in "Player email", with: email
    click_on "Create Faction"
end

def user_creates_asset_from_faction_page name, value, covert: false
  # User must be on a faction's create view to use this step
  click_on "create asset"
  fill_in "Name", with: name
  fill_in "Value", with: value
  check "Covert" if covert
  click_on "Submit Build"
end

def user_sets_order_for_asset asset, action, target
  within :xpath, section_containing_header("h4", asset) do
    select action, from: "Action"
    select target, from: "Target"
  end
end

def section_containing_header header_type, header_text
  <<-XPATH
  //#{header_type}[text()="#{header_text}"]/..
  XPATH
end
