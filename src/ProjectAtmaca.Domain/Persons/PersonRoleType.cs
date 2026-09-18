namespace ProjectAtmaca.Domain.Persons;

public enum PersonRoleType
{
    Prospect = 1,
    Player = 2,
    Coach = 3,
    TechnicalStaff = 4,
    MedicalStaff = 5,
    AdministrativeStaff = 6,
    Parent = 7,
    Supporter = 8
}

public enum PersonRoleStatus
{
    Active = 1,
    Inactive = 2,
    Completed = 3,
    Suspended = 4
}
