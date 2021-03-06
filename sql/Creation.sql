-- Clear out existing data
drop table info;
drop table gamepackageneutrals; 
drop table gamepackageschemes; 
drop table gamepackagemasterminds; 
drop table gamepackageadversaries; 
drop table gamepackagehenchmen; 
drop table gamepackageallies;
drop table schemeabilities; 
drop table mastermindabilities; 
drop table adversaryabilities; 
drop table henchmanabilities; 
drop table allyclasses; 
drop table allyabilities; 
drop table abilities; 
drop table gamepackages; 
drop table schemetwistrequirements;
drop table matchedcardrequirements;
drop table twistrequirements; 
drop table cardrequirements;
drop table cardsettypes; 
drop table neutrals; 
drop table schemes; 
drop table masterminds; 
drop table adversaries; 
drop table henchmen; 
drop table allies; 
drop table teams; 
drop table classes; 
drop table basemaps; 
drop table packagetypes; 

-- Create the packagetypes table
create table packagetypes (
    PackageTypeId int auto_increment primary key,
    Name varchar(25) not null,
    unique index packagetypes_Name (Name)
) engine=INNODB;

insert into packagetypes (Name) values ("BaseGame"), ("LargeExpansion"), ("SmallExpansion");

-- Create the basemaps table
create table basemaps (
    BaseMapId int auto_increment primary key,
    Name varchar(25) not null,
    unique index basemaps_Name (Name)
) engine=INNODB;

insert into basemaps (Name) values ("Legendary"), ("Villains");

-- Create the classes table
create table classes (
    ClassId int auto_increment primary key,
    Name varchar(50) not null,
    ImagePath varchar(500) not null
) engine=INNODB;

-- Create the teams table
create table teams (
    TeamId int auto_increment primary key,
    Name varchar(50) not null,
    ImagePath varchar(500) not null,
    unique index teams_Name (Name)
) engine=INNODB;

-- Create the allies table (requires `teams`)
create table allies (
    AllyId int auto_increment primary key,
    Name varchar(50) not null,
    TeamId int not null,
    unique index allies_Name_TeamId (Name, TeamId),
	constraint ally_TeamId foreign key (TeamId) references teams(TeamId) on update cascade on delete cascade
) engine=INNODB; 

-- Create the henchmen table
create table henchmen (
    HenchmanId int auto_increment primary key,
    Name varchar(50) not null,
    unique index henchmen_Name (Name)
) engine=INNODB;

-- Create the adversaries table
create table adversaries (
    AdversaryId int auto_increment primary key,
    Name varchar(50) not null,
    unique index adversaries_Name (Name)
) engine=INNODB;

-- Create the masterminds table
create table masterminds (
    MastermindId int auto_increment primary key,
    Name varchar(50) not null,
    HasEpicSide bool not null,
    unique index masterminds_Name (Name)
) engine=INNODB;

-- Create the schemes table
create table schemes (
    SchemeId int auto_increment primary key,
    Name varchar(100) not null,
    HasEpicSide bool not null,
    unique index schemes_Name (Name)
) engine=INNODB;

-- Create the neutrals table
create table neutrals (
    NeutralId int auto_increment primary key,   
    Name varchar(50) not null
) engine=INNODB;


-- Create the cardsettypes table
create table cardsettypes (
    CardSetTypeId int auto_increment primary key,
    Name varchar(50) not null
) engine=INNODB;

insert into cardsettypes (Name) values ("Adversary"), ("Ally"), ("Mastermind"), ("Neutral"), ("Bystander"), ("Henchman"), ("Scheme");

-- Create the cardrequirements table
create table cardrequirements (
    CardRequirementId int auto_increment primary key,
    AdditionalCardSetCount int default 0,
    AdditionalCardSetId int default 0,
    AdditionalCardSetName varchar(50) default null,
    CardSetTypeId int not null,
    constraint cardrequirement_CardSetTypeId foreign key (CardSetTypeId) references cardsettypes(CardSetTypeId) on update cascade on delete cascade
) engine=INNODB;

-- Create the matchedcardrequirements table
create table matchedcardrequirements (
    CardRequirementId int,
    OwnerId int,
    NumberOfPlayers int,
    CardSetTypeId int not null,
    primary key (CardRequirementId, OwnerId, NumberOfPlayers, CardSetTypeId),
    constraint matchedcardrequirements_CardRequirementId foreign key (CardRequirementId) references cardrequirements(CardRequirementId) on update cascade on delete cascade,
    constraint matchedcardrequirements_CardSetTypeId foreign key (CardSetTypeId) references cardsettypes(CardSetTypeId) on update cascade on delete cascade
) engine=INNODB;

-- Create the twistrequirements table
create table twistrequirements (
    TwistRequirementId int auto_increment primary key,
    TwistCount int not null,
    IsAllowed bool default true
) engine=INNODB;

-- Create the schemetwistrequirements table
create table schemetwistrequirements (
    SchemeId int,
    TwistRequirementId int,
    NumberOfPlayers int,
    primary key (SchemeId, TwistRequirementId, NumberOfPlayers),
    constraint schemetwists_SchemeId foreign key (SchemeId) references schemes(SchemeId) on update cascade on delete cascade,
    constraint schemetwists_TwistRequirementId foreign key (TwistRequirementId) references twistrequirements(TwistRequirementId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackages table (requires `packagetypes` and `basemaps`)
create table gamepackages(
    GamePackageId int auto_increment primary key,
    Name varchar(50) not null,
    CoverImage varchar(255),
    PackageTypeId int not null,
    BaseMapId int not null,
    constraint gamepackage_PackageTypeId foreign key (PackageTypeId) references packagetypes(PackageTypeId) on update cascade on delete cascade,
    constraint gamepackage_BaseMapId foreign key (BaseMapId) references basemaps(BaseMapId) on update cascade on delete cascade
) engine=INNODB;

-- Create the abilities table (requires `gamepackages`)
create table abilities (
    AbilityId int auto_increment primary key,
    Name varchar(50) not null,
    Description text not null,
    GamePackageId int,
    unique index abilites_Name_GamePackageId (Name, GamePackageId),
    constraint ability_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade
) engine=INNODB;

-- Create the allyabilities table (requires `allies` and `abilities`)
create table allyabilities (
    AllyId int not null,
    AbilityId int not null,
    primary key (AllyId, AbilityId),
    constraint allyabilities_AllyId foreign key (AllyId) references allies(AllyId) on update cascade on delete cascade,
    constraint allyabilities_AbilityId foreign key (AbilityId) references abilities(AbilityId) on update cascade on delete cascade
) engine=INNODB;

-- Create the allyclasses table (requires `allies` and `classes`)
create table allyclasses (
    AllyId int not null,
    ClassId int not null,
    CardCount int not null,
    primary key (AllyId, ClassId, CardCount),
    constraint allyclasses_AllyId foreign key (AllyId) references allies(AllyId) on update cascade on delete cascade,
    constraint allyclasses_ClassId foreign key (ClassId) references classes(ClassId) on update cascade on delete cascade
) engine=INNODB;

-- Create the henchmanabilities table (requires `henchmen` and `abilities`)
create table henchmanabilities (
    HenchmanId int not null,
    AbilityId int not null,
    primary key (HenchmanId, AbilityId),
    constraint henchmanabilities_HenchmanId foreign key (HenchmanId) references henchmen(HenchmanId) on update cascade on delete cascade,
    constraint henchmanabilities_AbilityId foreign key (AbilityId) references abilities(AbilityId) on update cascade on delete cascade
) engine=INNODB;

-- Create the adversaryabilities table (requires `adversaries` and `abilities`)
create table adversaryabilities (
    AdversaryId int not null,
    AbilityId int not null,
    primary key (AdversaryId, AbilityId),
    constraint adversaryabilities_AdversaryId foreign key (AdversaryId) references adversaries(AdversaryId) on update cascade on delete cascade,
    constraint adversaryabilities_AbilityId foreign key (AbilityId) references abilities(AbilityId) on update cascade on delete cascade
) engine=INNODB;

-- Create the mastermindabilities table (requires `masterminds` and `abilities`)
create table mastermindabilities (
    MastermindId int not null,
    AbilityId int not null,
    primary key (MastermindId, AbilityId),
    constraint mastermindabilities_MastermindId foreign key (MastermindId) references masterminds(MastermindId) on update cascade on delete cascade,
    constraint mastermindabilities_AbilityId foreign key (AbilityId) references abilities(AbilityId) on update cascade on delete cascade
) engine=INNODB;

-- Create the schemeabilities table (requires `schemes` and `abilities`)
create table schemeabilities (
    SchemeId int not null,
    AbilityId int not null,
    primary key (SchemeId, AbilityId),
    constraint schemeabilities_SchemeId foreign key (SchemeId) references schemes(SchemeId) on update cascade on delete cascade,
    constraint schemeabilities_AbilityId foreign key (AbilityId) references abilities(AbilityId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackageallies table (requires `gamepackages` and `allies`)
create table gamepackageallies (
    GamePackageId int not null,
    AllyId int not null,
    primary key (GamePackageId, AllyId),
    constraint gamepackageallies_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade,
    constraint gamepackageallies_AllyId foreign key (AllyId) references allies(AllyId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackagehenchmen table (requires `gamepackages` and `henchmen`)
create table gamepackagehenchmen (
    GamePackageId int not null,
    HenchmanId int not null,
    primary key (GamePackageId, HenchmanId),
    constraint gamepackagehenchmen_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade,
    constraint gamepackagehenchmen_HenchmanId foreign key (HenchmanId) references henchmen(HenchmanId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackageadversaries table (requires `gamepackages` and `adversaries`)
create table gamepackageadversaries (
    GamePackageId int not null,
    AdversaryId int not null,
    primary key (GamePackageId, AdversaryId),
    constraint gamepackageadversaries_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade,
    constraint gamepackageadversaries_AdversaryId foreign key (AdversaryId) references adversaries(AdversaryId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackagemasterminds table (requires `gamepackages` and `masterminds`)
create table gamepackagemasterminds (
    GamePackageId int not null,
    MastermindId int not null,
    primary key (GamePackageId, MastermindId),
    constraint gamepackagemasterminds_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade,
    constraint gamepackagemasterminds_MastermindId foreign key (MastermindId) references masterminds(MastermindId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackageschemes table (requires `gamepackages` and `schemes`)
create table gamepackageschemes (
    GamePackageId int not null,
    SchemeId int not null,
    primary key (GamePackageId, SchemeId),
    constraint gamepackageschemes_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade,
    constraint gamepackageschemes_SchemeId foreign key (SchemeId) references schemes(SchemeId) on update cascade on delete cascade
) engine=INNODB;

-- Create the gamepackageneutrals table (requires `gamepackages` and `neutrals`)
create table gamepackageneutrals (
    GamePackageId int not null,
    NeutralId int not null,
    primary key (GamePackageId, NeutralId),
    constraint gamepackageneutrals_GamePackageId foreign key (GamePackageId) references gamepackages(GamePackageId) on update cascade on delete cascade,
    constraint gamepackageneutrals_NeutralId foreign key (NeutralId) references neutrals(NeutralId) on update cascade on delete cascade
) engine=INNODB;

-- Create the info table for information about the database
create table info (
    Version int not null
) engine=INNODB;

insert into info (Version) values (1);