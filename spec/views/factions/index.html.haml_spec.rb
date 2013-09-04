require "spec_helper"

describe "factions/index.html.haml" do
  before do
    Turn.stub(:current).and_return current_turn
    Turn.stub(:count).and_return turn_count
    render
  end
  let(:turn_count) { 1 }

  context "when there are no turns in progress" do
    let(:current_turn) { nil }

    it "shows a link to create a faction" do
      expect(rendered).to include "create faction"
    end

    it "does not show a link to set orders" do
      expect(rendered).to_not include "Set orders"
    end

    context "and no turns have been completed" do
      let(:turn_count) { 0 }
      it "shows a button to start the first turn" do
        expect(rendered).to include "Start Turn #1"
      end
    end

    context "and one turn has been completed" do
      let(:turn_count) { 1 }
      it "shows a button to start the next turn" do
        expect(rendered).to include "Start Turn #2"
      end
    end
  end

  context "when there is a turn in progress" do
    let(:current_turn) do
      stub_model Turn,
                 id: 2,
                 state: :in_progress,
                 number: 2,
                 model_name: 'Turn'
    end

    it "does not show a button to start a turn" do
      expect(rendered).to_not include "Start Turn"
    end

    it "shows a link to set orders" do
      expect(rendered).to include "Set orders"
    end

    it "shows a button to process the turn" do
      expect(rendered).to include "Process Turn #2"
    end
  end
end
