require 'spec_helper'

describe Order do

  describe "#calculate_result" do
    let(:order) { FactoryGirl.build(:order, asset: asset, target: target) }
    let(:asset) { FactoryGirl.build(:asset, value: asset_value) }
    let(:target) { FactoryGirl.build(:asset, value: target_value) }
    subject { order.result }
    before { order.calculate_result }

    context "when the asset value equals the target value" do
      let(:asset_value) { 2 }
      let(:target_value) { 2 }
      it { should == :failure }
    end

    context "when the asset value exceeds the target value" do
      let(:asset_value) { 3 }
      let(:target_value) { 2 }
      it { should == :success }
    end

    context "when the asset value is less than the target value" do
      let(:asset_value) { 1 }
      let(:target_value) { 2 }
      it { should == :failure }
    end
  end
end
