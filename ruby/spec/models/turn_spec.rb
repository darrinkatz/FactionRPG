require 'spec_helper'

describe Turn do

  describe ".current" do
    subject { Turn.current }

    context "when there are no turns" do
      it { should be_nil }
    end

    context "when there is a turn in progress" do
      before do
        FactoryGirl.create(:turn, number: 1, state: :in_progress)
      end
      it { should be_a Turn }
    end

    context "when all turns have been processed" do
      before do
        FactoryGirl.create(:turn, number: 1, state: :processed)
      end
      it { should be_nil }
    end
  end

  describe ".start_next_turn" do
    subject { Turn.start_next_turn }
    it { should be_a Turn }
    its(:state) { should == :in_progress }

    context "when there are no previous Turns" do
      its(:number) { should == 1 }
    end

    context "when there is a previous Turn" do
      before { FactoryGirl.create(:turn, state: :processed, number: 1) }
      its(:number) { should == 2 }
    end

    context "when there is an Asset in play" do
      before { FactoryGirl.create(:asset) }
      its(:orders) { should_not be_empty }
      its(:orders) { should be_all{|o| o.type == "Attack"} }
      its(:orders) { should be_all{|o| o.target.nil?} }
    end
  end
end
