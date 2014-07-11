FactoryGirl.define do
  factory :asset do
    association :faction
    name "Test Asset"
    value 1
    covert false
  end
end
