using CsvHelper;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;

namespace Wyhb.Joe.Tools
{
    public class Util
    {
        #region ToDataTable

        /// <summary>
        /// ToDataTable
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="classList"></param>
        /// <returns></returns>
        public static DataTable ToDataTable<T>(List<T> clazzList)
        {
            var props = TypeDescriptor.GetProperties(typeof(T));
            var dt = new DataTable();
            for (var i = 0; i < props.Count; i++)
            {
                dt.Columns.Add(props[i].Name, props[i].PropertyType);
            }
            var values = new object[props.Count].ToList();

            clazzList.ToList().ForEach(item =>
            {
                values.ForEach(value =>
                {
                    var idx = values.IndexOf(value);
                    if (props[idx].PropertyType == typeof(DateTime))
                    {
                        value = ((DateTime)props[idx].GetValue(item)).ToUniversalTime();
                    }
                    else
                    {
                        value = props[idx].GetValue(item);
                    }
                });
                dt.Rows.Add(values);
            });
            return dt;
        }

        #endregion ToDataTable

        #region SerializationDeepFieldClone

        public object SerializationDeepFieldClone(object original)
        {
            return Cloning.SerializationCloner.DeepFieldClone(original);
        }

        #endregion SerializationDeepFieldClone

        #region SerializationDeepPropertyClone

        public object SerializationDeepPropertyClone(object original)
        {
            return Cloning.SerializationCloner.DeepPropertyClone(original);
        }

        #endregion SerializationDeepPropertyClone

        #region ReflectionDeepFieldClone

        public object ReflectionClonerDeepFieldClone(object original)
        {
            return Cloning.ReflectionCloner.DeepFieldClone(original);
        }

        #endregion ReflectionDeepFieldClone

        #region ReflectionDeepPropertyClone

        public object ReflectionDeepPropertyClone(object original)
        {
            return Cloning.ReflectionCloner.DeepPropertyClone(original);
        }

        #endregion ReflectionDeepPropertyClone

        #region ExpressionTreeDeepFieldClone

        public object ExpressionTreeDeepFieldClone(object original)
        {
            return Cloning.ExpressionTreeCloner.DeepFieldClone(original);
        }

        #endregion ExpressionTreeDeepFieldClone

        #region ExpressionTreeDeepPropertyClone

        public object ExpressionTreeDeepPropertyClone(object original)
        {
            return Cloning.ExpressionTreeCloner.DeepPropertyClone(original);
        }

        #endregion ExpressionTreeDeepPropertyClone

        #region DataTableToCsv

        /// <summary>
        /// DataTableToCsv
        /// </summary>
        /// <param name="path">FilePath</param>
        /// <param name="dt">DataTable</param>
        /// <param name="append">Append</param>
        /// <param name="encoding">Encoding</param>
        /// <param name="quoteAllFields">QuoteAllFields</param>
        public void DataTableToCsv(string path, DataTable dt, bool append, Encoding encoding, bool quoteAllFields)
        {
            using (var textWriter = new StreamWriter(path, append, encoding))
            {
                using (var csv = new CsvWriter(textWriter))
                {
                    csv.Configuration.QuoteAllFields = quoteAllFields;
                    dt.Columns.Cast<DataColumn>().ToList().ForEach(column =>
                    {
                        csv.WriteField(column.ColumnName);
                    });

                    dt.Rows.Cast<DataRow>().ToList().ForEach(row =>
                    {
                        for (var idx = 0; idx < dt.Columns.Count; idx++)
                        {
                            csv.WriteField(row[idx]);
                        }
                        csv.NextRecord();
                    });
                }
            }
        }

        #endregion DataTableToCsv

        #region CsvToDataTable

        /// <summary>
        /// CsvToDataTable
        /// </summary>
        /// <param name="path">FilePath</param>
        /// <returns>DataTable</returns>
        public DataTable CsvToDataTable(string path)
        {
            var dt = new DataTable();
            using (var textReader = new StreamReader(path))
            {
                using (var csv = new CsvReader(textReader))
                {
                    while (csv.Read())
                    {
                        var row = dt.NewRow();
                        dt.Columns.Cast<DataColumn>().ToList().ForEach(col =>
                        {
                            row[col.ColumnName] = csv.GetField(col.DataType, col.ColumnName);
                        });
                        dt.Rows.Add(row);
                    }
                }
            }
            return dt;
        }

        #endregion CsvToDataTable

        #region ListToCsv

        /// <summary>
        /// ListToCsv
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="path">FilePath</param>
        /// <param name="records">List<object></param>
        /// <param name="append">Append</param>
        /// <param name="encoding">Encoding</param>
        /// <param name="quoteAllFields">QuoteAllFields</param>
        public void ListToCsv<T>(string path, List<T> records, bool append, Encoding encoding, bool quoteAllFields)
        {
            using (var textWriter = new StreamWriter(path, append, encoding))
            {
                using (var csv = new CsvWriter(textWriter))
                {
                    csv.Configuration.QuoteAllFields = quoteAllFields;
                    csv.WriteRecords(records);
                }
            }
        }

        #endregion ListToCsv

        #region CsvToList

        /// <summary>
        /// CsvToList
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="path">FilePath</param>
        /// <returns>List<T></returns>
        public List<T> CsvToList<T>(string path)
        {
            var t = new List<T>();
            using (var textReader = new StreamReader(path))
            {
                using (var csv = new CsvReader(textReader))
                {
                    t = csv.GetRecords<T>().ToList();
                }
            }
            return t;
        }

        #endregion CsvToList

        #region DataTableToExcelWithInterop

        /// <summary>
        /// DataTableToExcelWithInterop
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="filePath">FilePath</param>
        public void DataTableToExcelWithInterop(DataTable dt, string filePath)
        {
            var ExcelApp = new Microsoft.Office.Interop.Excel.Application();
            try
            {
                ExcelApp.DisplayAlerts = false;
                ExcelApp.Visible = false;
                var wb = ExcelApp.Workbooks.Add();
                var ws = wb.Sheets[1];
                ws.Select(Type.Missing);
                for (var col = 0; col < dt.Columns.Count; col++)
                {
                    object[,] obj = new object[dt.Rows.Count + 1, 1];
                    obj[0, 0] = dt.Columns[col].ColumnName;
                    for (var row = 0; row < dt.Rows.Count; row++)
                    {
                        obj[row + 1, 0] = dt.Rows[row][col].ToString();
                    }
                    var rgn = ws.Range[ws.Cells[1, col + 1], ws.Cells[dt.Rows.Count + 1, col + 1]];
                    rgn.Font.Size = 10;
                    rgn.Font.Name = "Meiryo UI";
                    var dtcol = dt.Columns[col];
                    if (dtcol.DataType.ToString() == "System.String")
                    {
                        rgn.NumberFormatLocal = "@";
                        rgn.Value2 = obj;
                    }
                    else
                    {
                        rgn.Value2 = obj;
                    }
                }
                wb.SaveAs(filePath);
                wb.Close(false);
            }
            finally
            {
                ExcelApp.Quit();
            }
        }

        #endregion DataTableToExcelWithInterop

        #region DataTableToExcelWithClosedXML

        /// <summary>
        /// DataTableToExcelWithClosedXML
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="filePath">FilePath</param>
        public void DataTableToExcelWithClosedXML(DataTable dt, string filePath)
        {
            var wb = new ClosedXML.Excel.XLWorkbook();
            wb.Worksheets.Add(dt, "sheet1");
            wb.SaveAs(filePath);
        }

        #endregion DataTableToExcelWithClosedXML

        #region DataTableToExcelWithNPOI

        /// <summary>
        /// DataTableToExcelWithNPOI
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="filePath">FilePath</param>
        /// <param name="extension">Extension</param>
        public void DataTableToExcelWithNPOI(DataTable dt, string filePath, string extension)
        {
            using (var fs = new FileStream(filePath, FileMode.Create))
            {
                using (var ms = new MemoryStream())
                {
                    NPOI.SS.UserModel.IWorkbook workbook;
                    if (string.IsNullOrEmpty(extension) || "XLSX".Equals(extension.ToUpper()))
                    {
                        workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
                    }
                    else
                    {
                        workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();
                    }
                    var sheet = workbook.CreateSheet("sheet1");

                    dt.Columns.Cast<DataColumn>().ToList().ForEach(col =>
                    {
                        sheet.CreateRow(0).CreateCell(col.Ordinal).SetCellValue(col.ColumnName.ToString());
                    });
                    dt.Rows.Cast<DataRow>().ToList().Select((row, idx) => new { Idx = idx, Row = row }).ToList().ForEach(x =>
                    {
                        dt.Columns.Cast<DataColumn>().ToList().ForEach(col =>
                        {
                            sheet.CreateRow(x.Idx + 1).CreateCell(col.Ordinal).SetCellValue(x.Row[col.ColumnName].ToString());
                        });
                    });
                    workbook.Write(ms);
                    ms.Flush();
                    ms.Position = 0;
                    ms.WriteTo(fs);
                }
            }
        }

        #endregion DataTableToExcelWithNPOI

        #region ExcelToDataTableWithNPOI

        /// <summary>
        /// ExcelToDataTableWithNPOI
        /// </summary>
        /// <param name="dt">DataTable</param>
        /// <param name="filePath">FilePath</param>
        /// <param name="extension">Extension</param>
        public DataTable ExcelToDataTableWithNPOI(string filePath, string extension)
        {
            var dt = new DataTable();
            NPOI.SS.UserModel.IWorkbook workbook;
            if (string.IsNullOrEmpty(extension) || "XLSX".Equals(extension.ToUpper()))
            {
                workbook = new NPOI.XSSF.UserModel.XSSFWorkbook();
            }
            else
            {
                workbook = new NPOI.HSSF.UserModel.HSSFWorkbook();
            }
            using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                var sheet = workbook.GetSheetAt(0);
                sheet.GetRow(0).ToList().ForEach(hdrCell =>
                {
                    dt.Columns.Add(hdrCell.ToString());
                });
                var rows = sheet.GetRowEnumerator();
                while (rows.MoveNext())
                {
                    var row = (NPOI.SS.UserModel.IRow)rows.Current;
                    var dataRow = dt.NewRow();
                    row.ToList().Select((dtlCell, idx) => new { Idx = idx, DtlCell = dtlCell }).ToList().ForEach(x =>
                        {
                            if (x.DtlCell != null)
                            {
                                dataRow[x.Idx] = x.DtlCell.ToString();
                            }
                        });
                    dt.Rows.Add(dataRow);
                }
            }
            return dt;
        }

        #endregion ExcelToDataTableWithNPOI

        #region MergeObject

        /// <summary>
        /// MergeObject
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="objA">object</param>
        /// <param name="objB">object</param>
        /// <returns></returns>
        public static T MergeObject<T>(T objA, T objB)
        {
            var objResult = Activator.CreateInstance(typeof(T));
            var allProperties = typeof(T).GetProperties().Where(x => x.CanRead && x.CanWrite);
            allProperties.ToList().ForEach(pi =>
            {
                object defaultValue;
                if (pi.PropertyType.IsValueType)
                {
                    defaultValue = Activator.CreateInstance(pi.PropertyType);
                }
                else
                {
                    defaultValue = null;
                }

                var value = pi.GetValue(objB, null);

                if (value != null && !value.Equals(defaultValue) || (defaultValue != null && !defaultValue.Equals(value)))
                {
                    pi.SetValue(objResult, value, null);
                }
                else
                {
                    value = pi.GetValue(objA, null);

                    if (value != null && !value.Equals(defaultValue) || (defaultValue != null && !defaultValue.Equals(value)))
                    {
                        pi.SetValue(objResult, value, null);
                    }
                }
            });
            return (T)objResult;
        }

        #endregion MergeObject

        #region MergeObjectList

        /// <summary>
        /// MergeObjectList
        /// </summary>
        /// <typeparam name="T">List<T></typeparam>
        /// <param name="objAList">object List</param>
        /// <param name="objBList">object List</param>
        /// <returns></returns>
        public static List<T> MergeObjectList<T>(List<T> objAList, List<T> objBList)
        {
            var result = new List<T>();
            objAList.Select((objA, idx) => new { Idx = idx, Obj = objA }).ToList().ForEach(objA =>
                {
                    result.Add((T)MergeObject<T>(objA.Obj, objBList[objA.Idx]));
                });
            return result;
        }

        #endregion MergeObjectList

        #region DynamicMerge

        public static dynamic DynamicMerge(object item1, object item2)
        {
            if (item1 == null || item2 == null)
                return item1 ?? item2 ?? new ExpandoObject();

            dynamic expando = new ExpandoObject();
            var result = expando as IDictionary<string, object>;
            foreach (System.Reflection.PropertyInfo fi in item1.GetType().GetProperties())
            {
                object defaultValue;
                if (fi.PropertyType.IsValueType)
                {
                    defaultValue = Activator.CreateInstance(fi.PropertyType);
                }
                else
                {
                    defaultValue = null;
                }
                var value = fi.GetValue(item1, null);
                if (value != null && (value != null && !value.Equals(defaultValue) || defaultValue != null && !defaultValue.Equals(value)))
                {
                    result[fi.Name] = fi.GetValue(item1, null);
                }
            }
            foreach (System.Reflection.PropertyInfo fi in item2.GetType().GetProperties())
            {
                object defaultValue;
                if (fi.PropertyType.IsValueType)
                {
                    defaultValue = Activator.CreateInstance(fi.PropertyType);
                }
                else
                {
                    defaultValue = null;
                }
                var value = fi.GetValue(item2, null);
                if (value != null && (value != null && !value.Equals(defaultValue) || defaultValue != null && !defaultValue.Equals(value)))
                {
                    result[fi.Name] = fi.GetValue(item2, null);
                }
            }
            return result;
        }

        #endregion DynamicMerge

        #region DynamicMergeList

        public static dynamic DynamicMergeList(List<object> item1Lst, List<object> item2Lst)
        {
            var result = new List<dynamic>();
            item1Lst.Select((Item1, Idx) => new { Idx, Item1 }).ToList().ForEach(item =>
            {
                result.Add(DynamicMerge(item.Item1, item2Lst[item.Idx]));
            });
            return result;
        }

        #endregion DynamicMergeList

        #region MergeHdrDataToDtlEntity

        /// <summary>
        /// MergeHdrDataToDtlEntity
        /// </summary>
        /// <typeparam name="T">T</typeparam>
        /// <param name="hdrData">HeaderData</param>
        /// <param name="dtlEntity">DetailEntity</param>
        /// <returns></returns>
        public static List<T> MergeHdrDataToDtlEntity<T>(object hdrData, List<T> dtlEntity)
        {
            dtlEntity.All(d =>
            {
                d.GetType().GetProperties().All(dpi =>
                 {
                     hdrData.GetType().GetProperties().All(hpi =>
                     {
                         if (hpi.Name.Equals(dpi.Name))
                         {
                             dpi.SetValue(d, hpi.GetValue(hdrData, null));
                         }
                         return true;
                     });
                     return true;
                 });
                return true;
            });
            return dtlEntity;
        }

        #endregion MergeHdrDataToDtlEntity
    }
}