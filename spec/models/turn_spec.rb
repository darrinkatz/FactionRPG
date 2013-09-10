require 'spec_helper'

describe Turn do

  describe ".current" do
    subject { Turn.current }

    context "when there are no turns" do
      it { should be_nil }
    end

    context "when there is at least one turn" do
      before { FactoryGirl.create(:turn) }
      it { should_not be_nil }
      it { should == Turn.last }
    end
  end

  describe ".start_next_turn" do
    subject { Turn.start_next_turn }
    it { should be_a Turn }

    context "when there are no previous Turns" do
      its(:number) { should == 1 }
    end

    context "when there is a previous Turn" do
      before { FactoryGirl.create(:turn, number: 1) }
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
