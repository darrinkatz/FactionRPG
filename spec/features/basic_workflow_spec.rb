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

    expect(page).to have_content "create asset"
  end

end
