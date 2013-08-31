require "spec_helper"

feature "create faction:" do

  scenario "sees faction list" do
    visit factions_path
    expect(page).to have_content "factions"
  end

  scenario "user creates faction" do
    visit factions_path
    click_on "create faction"

    expect(page).to have_content "create a faction"

    fill_in "Faction name", with: "House Dimir"
    fill_in "Player email", with: "darrinkatz@gmail.com"
    click_on "Create Faction"

    expect(page).to have_content "House Dimir"
    expect(page).to have_content "darrinkatz@gmail.com"

    click_on "create asset"
    fill_in "Name", with: "Szadek"
    fill_in "Value", with: 5
    check "Covert"
    click_on "Submit Build"

    expect(page).to have_content "House Dimir"
    expect(page).to have_content "5-Szadek (covert)"
  end

end
