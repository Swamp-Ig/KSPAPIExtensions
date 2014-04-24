using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KSPAPIExtensions.Utils
{
    public class EngineWrapper
    {
        public enum ModuleType
        {
            MODULEENGINES,
            MODULEENGINESFX,
            MODULERCS
        }

        private ModuleType type;
        private ModuleEngines mE;
        private ModuleEnginesFX mEFX;

        public EngineWrapper(Part part)
        {
            if ((mEFX = part.transform.GetComponent<ModuleEnginesFX>()) != null)
                type = ModuleType.MODULEENGINESFX;
            else if ((mE = part.transform.GetComponent<ModuleEngines>()) != null)
                type = ModuleType.MODULEENGINES;
            else
                throw new ArgumentException("Unable to find engine-like module");
        }

        public EngineWrapper(ModuleEngines mod)
        {
            mE = mod;
            type = ModuleType.MODULEENGINES;
        }

        public EngineWrapper(ModuleEnginesFX mod)
        {
            mEFX = mod;
            type = ModuleType.MODULEENGINESFX;
        }

        public static implicit operator PartModule(EngineWrapper wrapper)
        {
            return (PartModule)wrapper.mE ?? wrapper.mEFX;
        }

        public static explicit operator ModuleEngines(EngineWrapper wrapper)
        {
            return wrapper.mE;
        }

        public static explicit operator ModuleEnginesFX(EngineWrapper wrapper)
        {
            return wrapper.mEFX;
        }

        public ModuleType Type { get { return type; } }

        public List<Propellant> propellants
        {
            get
            {
                switch(type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.propellants;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.propellants;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public void SetupPropellant()
        {
            switch (type)
            {
                case ModuleType.MODULEENGINES:
                    mE.SetupPropellant();
                    break;
                case ModuleType.MODULEENGINESFX:
                    mEFX.SetupPropellant();
                    break;
                default:
                    throw new InvalidProgramException();
            }
        }
        public BaseActionList Actions
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.Actions;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.Actions;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public bool getIgnitionState
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.getIgnitionState;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.getIgnitionState;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public bool EngineIgnited
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.EngineIgnited;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.EngineIgnited;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.EngineIgnited = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.EngineIgnited = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public FloatCurve atmosphereCurve
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.atmosphereCurve;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.atmosphereCurve;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.atmosphereCurve = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.atmosphereCurve = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public FloatCurve velocityCurve
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.velocityCurve;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.velocityCurve;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.velocityCurve = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.velocityCurve = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public bool useVelocityCurve
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.useVelocityCurve;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.useVelocityCurve;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.useVelocityCurve = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.useVelocityCurve = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public float maxThrust
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.maxThrust;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.maxThrust;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.maxThrust = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.maxThrust = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public float minThrust
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.minThrust;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.minThrust;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.minThrust = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.minThrust = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
        public float heatProduction
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.heatProduction;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.heatProduction;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.heatProduction = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.heatProduction = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }

        public float g
        {
            get
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        return mE.g;
                    case ModuleType.MODULEENGINESFX:
                        return mEFX.g;
                    default:
                        throw new InvalidProgramException();
                }
            }
            set
            {
                switch (type)
                {
                    case ModuleType.MODULEENGINES:
                        mE.g = value;
                        break;
                    case ModuleType.MODULEENGINESFX:
                        mEFX.g = value;
                        break;
                    default:
                        throw new InvalidProgramException();
                }
            }
        }
    }
}
