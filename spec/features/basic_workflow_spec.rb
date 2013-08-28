require "spec_helper"

feature "create faction:" do
	scenario "sees faction list" do
		visit factions_path
		expect(page).to have_content "factions"
	end

	scenario "user goes to create faction page" do
		visit factions_path
		click_on "create faction"
		expect(page).to have_content "create a faction"
	end
end