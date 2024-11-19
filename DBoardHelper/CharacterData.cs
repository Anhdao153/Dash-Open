namespace ADashboard
{
    public class CharacterData
    {
        public string clientId { get; set; }
        public string name { get; set; }
        public string level { get; set; }
        public string masterLevel { get; set; }
        public string @class { get; set; }
        public string hp { get; set; }
        public string maxHp { get; set; }
        public string mp { get; set; }
        public string maxMp { get; set; }
        public string shield { get; set; }
        public string maxShield { get; set; }
        public string skillMana { get; set; }
        public string maxSkillMana { get; set; }
        public string exp { get; set; }
        public string nextExp { get; set; }
        public string posX { get; set; }
        public string posY { get; set; }
        public string terrainIndex { get; set; }
        public string serverIndex { get; set; }
        public string str { get; set; }
        public string agi { get; set; }
        public string vit { get; set; }
        public string ene { get; set; }
        public string cmd { get; set; }
        public string vipAddress { get; set; }

        public void Update(CharacterData newData)
        {
            name = newData.name;
            level = newData.level;
            masterLevel = newData.masterLevel;
            @class = newData.@class;
            hp = newData.hp;
            maxHp = newData.maxHp;
            mp = newData.mp;
            maxMp = newData.maxMp;
            shield = newData.shield;
            maxShield = newData.maxShield;
            skillMana = newData.skillMana;
            maxSkillMana = newData.maxSkillMana;
            exp = newData.exp;
            nextExp = newData.nextExp;
            posX = newData.posX;
            posY = newData.posY;
            terrainIndex = newData.terrainIndex;
            serverIndex = newData.serverIndex;
            str = newData.str;
            agi = newData.agi;
            vit = newData.vit;
            ene = newData.ene;
            cmd = newData.cmd;
            vipAddress = newData.vipAddress;
        }

        public override bool Equals(object obj)
        {
            if (obj is CharacterData other)
            {
                return clientId == other.clientId &&
                       name == other.name &&
                       level == other.level &&
                       masterLevel == other.masterLevel &&
                       @class == other.@class &&
                       hp == other.hp &&
                       maxHp == other.maxHp &&
                       mp == other.mp &&
                       maxMp == other.maxMp &&
                       shield == other.shield &&
                       maxShield == other.maxShield &&
                       skillMana == other.skillMana &&
                       maxSkillMana == other.maxSkillMana &&
                       exp == other.exp &&
                       nextExp == other.nextExp &&
                       posX == other.posX &&
                       posY == other.posY &&
                       terrainIndex == other.terrainIndex &&
                       serverIndex == other.serverIndex &&
                       str == other.str &&
                       agi == other.agi &&
                       vit == other.vit &&
                       ene == other.ene &&
                       cmd == other.cmd &&
                       vipAddress == other.vipAddress;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(clientId, name, level, masterLevel, @class, hp, maxHp, mp, maxMp, shield, maxShield, skillMana, maxSkillMana, exp, nextExp, posX, posY, terrainIndex, serverIndex, str, agi, vit, ene, cmd, vipAddress);
        }
    }
}