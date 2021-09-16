using System.Xml;
using System.Data;
using System.IO;
using UnityEngine;
using System;
public class ConfigFileUtils
{
    public const string CONFIG_FILE_PATH = "./Assets/Configuration/config.xml";

    public static void ReadFile(){

        if(!File.Exists(CONFIG_FILE_PATH)){
            Debug.Log("Configuration file doesn't exist, using default values.");
            using(XmlWriter writer = XmlWriter.Create(CONFIG_FILE_PATH)){
                writer.WriteStartDocument();
                writer.WriteStartElement("Settings");
                writer.WriteStartElement("MapSetup");
                writer.WriteElementString("map_n_district_x", Map_Setup.Instance.map_n_districts_x.ToString());
                writer.WriteElementString("map_n_district_y", Map_Setup.Instance.map_n_districts_y.ToString());
                writer.WriteElementString("n_entities", Map_Setup.Instance.n_entities.ToString());
                writer.WriteElementString("n_bus_lines", Map_Setup.Instance.n_bus_lines.ToString());
                writer.WriteElementString("frequency_district_0", Map_Setup.Instance.Frequency_District_0.ToString());
                writer.WriteElementString("frequency_district_1", Map_Setup.Instance.Frequency_District_1.ToString());
                writer.WriteElementString("frequency_district_2", Map_Setup.Instance.Frequency_District_2.ToString());
                writer.WriteElementString("frequency_district_3", Map_Setup.Instance.Frequency_District_3.ToString());
                writer.WriteEndElement();
                
                writer.WriteStartElement("MapSpawner");
                writer.WriteElementString("maxCarSpeed", Map_Spawner.instance.maxCarSpeed.ToString());
                writer.WriteElementString("maxBusSpeed", Map_Spawner.instance.maxBusSpeed.ToString());
                writer.WriteElementString("minimumTrafficLightTime", Map_Spawner.instance.minTrafficLightTime.ToString());
                writer.WriteElementString("maximumTrafficLightTime", Map_Spawner.instance.maxTrafficLightTime.ToString());
                writer.WriteEndElement();

                writer.WriteStartElement("MapVisual");
                writer.WriteElementString("differentTypeOfVehicles", Map_Visual.instance.differentTypeOfVehicles.ToString());
                writer.WriteEndElement();
                writer.WriteEndElement();
                writer.WriteEndDocument();

                writer.Flush();
            }
        }
        else if(!Map_Setup.Instance.overrideReadingConfigFile){
            using(XmlReader reader = XmlReader.Create(CONFIG_FILE_PATH)){

                reader.ReadToFollowing("map_n_district_x");
                Map_Setup.Instance.map_n_districts_x = reader.ReadElementContentAsInt();
                Map_Setup.Instance.map_n_districts_y = reader.ReadElementContentAsInt();
                Map_Setup.Instance.n_entities = reader.ReadElementContentAsInt();
                Map_Setup.Instance.n_bus_lines = reader.ReadElementContentAsInt();
                Map_Setup.Instance.Frequency_District_0 = reader.ReadElementContentAsInt();
                Map_Setup.Instance.Frequency_District_1 = reader.ReadElementContentAsInt();
                Map_Setup.Instance.Frequency_District_2 = reader.ReadElementContentAsInt();
                Map_Setup.Instance.Frequency_District_3 = reader.ReadElementContentAsInt();

                reader.ReadToFollowing("maxCarSpeed");
                Map_Spawner.instance.maxCarSpeed = reader.ReadElementContentAsInt();
                Map_Spawner.instance.maxBusSpeed = reader.ReadElementContentAsInt();
                Map_Spawner.instance.minTrafficLightTime = reader.ReadElementContentAsFloat();
                Map_Spawner.instance.maxTrafficLightTime = reader.ReadElementContentAsFloat();
                reader.ReadToFollowing("differentTypeOfVehicles");
                Map_Visual.instance.differentTypeOfVehicles = reader.ReadElementContentAsInt();
            }
        }
        return;
    }
}
