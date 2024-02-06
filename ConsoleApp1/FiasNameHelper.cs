using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace FIAS
{
    internal static class FiasNameHelper
    {

        internal static FiasNames GetFiasName(string fileName)

        {
            switch (GetDistrict(fileName))
            {
                case "AS_ADDR_OBJ_20":
                    return FiasNames.AS_ADDR_OBJ_20;

                case "AS_ADDR_OBJ_DIVISION_20":
                    return FiasNames.AS_ADDR_OBJ_DIVISION_20;

                case "AS_ADDR_OBJ_PARAMS_20":
                    return FiasNames.AS_ADDR_OBJ_PARAMS_20;

                case "AS_ADM_HIERARCHY_20":
                    return FiasNames.AS_ADM_HIERARCHY_20;

                case "AS_APARTMENTS_20":
                    return FiasNames.AS_APARTMENTS_20;

                case "AS_APARTMENTS_PARAMS_20":
                    return FiasNames.AS_APARTMENTS_PARAMS_20;

                case "AS_CARPLACES_20":
                    return FiasNames.AS_CARPLACES_20;

                case "AS_CARPLACES_PARAMS_20":
                    return FiasNames.AS_CARPLACES_PARAMS_20;

                case "AS_CHANGE_HISTORY_20":
                    return FiasNames.AS_CHANGE_HISTORY_20;

                case "AS_HOUSES_20":
                    return FiasNames.AS_HOUSES_20;

                case "AS_HOUSES_PARAMS_20":
                    return FiasNames.AS_HOUSES_PARAMS_20;

                case "AS_MUN_HIERARCHY_20":
                    return FiasNames.AS_MUN_HIERARCHY_20;

                case "AS_NORMATIVE_DOCS_20":
                    return FiasNames.AS_NORMATIVE_DOCS_20;

                case "AS_REESTR_OBJECTS_20":
                    return FiasNames.AS_REESTR_OBJECTS_20;

                case "AS_ROOMS_20":
                    return FiasNames.AS_ROOMS_20;

                case "AS_ROOMS_PARAMS_20":
                    return FiasNames.AS_ROOMS_PARAMS_20;

                case "AS_STEADS_20":
                    return FiasNames.AS_STEADS_20;

                case "AS_STEADS_PARAMS_20":
                    return FiasNames.AS_STEADS_PARAMS_20;

                case "AS_ADDHOUSE_TYPES_20":
                    return FiasNames.AS_ADDHOUSE_TYPES_20;

                case "AS_ADDR_OBJ_TYPES_20":
                    return FiasNames.AS_ADDR_OBJ_TYPES_20;

                case "AS_APARTMENT_TYPES_20":
                    return FiasNames.AS_APARTMENT_TYPES_20;

                case "AS_HOUSE_TYPES_20":
                    return FiasNames.AS_HOUSE_TYPES_20;

                case "AS_NORMATIVE_DOCS_KINDS_20":
                    return FiasNames.AS_NORMATIVE_DOCS_KINDS_20;

                case "AS_NORMATIVE_DOCS_TYPES_20":
                    return FiasNames.AS_NORMATIVE_DOCS_TYPES_20;

                case "AS_OBJECT_LEVELS_20":
                    return FiasNames.AS_OBJECT_LEVELS_20;

                case "AS_OPERATION_TYPES_20":
                    return FiasNames.AS_OPERATION_TYPES_20;

                case "AS_PARAM_TYPES_20":
                    return FiasNames.AS_PARAM_TYPES_20;

                case "AS_ROOM_TYPES_20":
                    return FiasNames.AS_ROOM_TYPES_20;

            }

            throw new ArgumentOutOfRangeException(fileName);
        }


        public static string GetDistrict(string fileName)
        {
            string[] keys = new string[] { "AS_ADDR_OBJ_20", "AS_ADDR_OBJ_DIVISION_20", "AS_ADDR_OBJ_PARAMS_20", "AS_ADM_HIERARCHY_20", "AS_APARTMENTS_20", "AS_APARTMENTS_PARAMS_20", "AS_CARPLACES_20", "AS_CARPLACES_PARAMS_20", "AS_CHANGE_HISTORY_20", "AS_HOUSES_20", "AS_HOUSES_PARAMS_20", "AS_MUN_HIERARCHY_20", "AS_NORMATIVE_DOCS_20", "AS_REESTR_OBJECTS_20", "AS_ROOMS_20", "AS_ROOMS_PARAMS_20", "AS_STEADS_20", "AS_STEADS_PARAMS_20", "AS_ADDHOUSE_TYPES_20", "AS_ADDR_OBJ_TYPES_20", "AS_APARTMENT_TYPES_20", "AS_HOUSE_TYPES_20", "AS_NORMATIVE_DOCS_KINDS_20", "AS_NORMATIVE_DOCS_TYPES_20", "AS_OBJECT_LEVELS_20", "AS_OPERATION_TYPES_20", "AS_PARAM_TYPES_20", "AS_ROOM_TYPES_20"};
            string sKeyResult = keys.FirstOrDefault<string>(s => fileName.Contains(s));
            return sKeyResult;
            
        }

        public static long VersionId;

      

    }
}
