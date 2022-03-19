using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace Lab2
{
    public partial class MainForm : Form
    {
        public static string connectionString = @"Data Source=DESKTOP-AP4AO3B\SQLEXPRESS;Initial Catalog=AddressTree;Integrated Security=True;Connect Timeout=30;Encrypt=False;TrustServerCertificate=False;ApplicationIntent=ReadWrite;MultiSubnetFailover=False;";

        public List<TreeNode> zeroLevelNodes = new List<TreeNode>();
        public List<TreeNode> firstLevelNodes = new List<TreeNode>();

        public MainForm()
        {
            InitializeComponent();
            добавитьToolStripMenuItem.Enabled = false;
            удалитьToolStripMenuItem.Enabled = false;
            редактироватьToolStripMenuItem.Enabled = false;
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            treeView.Nodes.Clear();
            добавитьToolStripMenuItem.Enabled = true;
            LoadRegions();
        }


        void LoadRegions()
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "select * from RegionInfo";
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    TreeNode tn = new TreeNode(dr["regionName"].ToString(), 2, 2);
                    tn.Tag = (short)dr["regionCode"];
                    treeView.Nodes.Add(tn);
                    zeroLevelNodes.Add(tn);
                }
            }
        }

        void LoadDistricts(short regionCode, TreeNode parent)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = @"select districtName, districtCode from DistrictInfo where regionCode = @regionCode";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@regionCode", regionCode);
                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    TreeNode tn = new TreeNode(dr["districtName"].ToString(), 0, 0);
                    tn.Tag = (short)dr["districtCode"];
                    parent.Nodes.Add(tn);
                    firstLevelNodes.Add(tn);
                }
            }
        }

        void LoadCities(short districtCode, TreeNode parent)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                var sql = @"select cityName, cityCode from CityInfo where districtCode = @districtCode";
                var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@districtCode", districtCode);
                var dr = cmd.ExecuteReader();

                while (dr.Read())
                {
                    TreeNode tn = new TreeNode(dr["cityName"].ToString(), 3, 3);
                    tn.Tag = (short)dr["cityCode"];
                    parent.Nodes.Add(tn);
                }
            }
        }

        
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (treeView.SelectedNode != null)
            {
                редактироватьToolStripMenuItem.Enabled = true;
                if (treeView.SelectedNode.Level == 2)
                    { добавитьToolStripMenuItem.Enabled = false; }
                    удалитьToolStripMenuItem.Enabled = true; 
                }
            else 
            {
                удалитьToolStripMenuItem.Enabled = false;
                редактироватьToolStripMenuItem.Enabled = false;
            }
        }

        private void treeView_MouseUp(object sender, MouseEventArgs e)
        {
            if (treeView.GetNodeAt(e.X, e.Y) == null)
            {
                treeView.SelectedNode = null;
            }
        }


        private void добавитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TreeNode node = treeView.SelectedNode;
            CreateUpdateForm createUpdateForm = new CreateUpdateForm(node);
            createUpdateForm.ShowDialog();

            string name = createUpdateForm.name;
            if (name != null)
            {
                if (node is null) { InsertRegion(name); }
                else
                {
                    switch (node.Level)
                    {
                        case 0:
                            {
                                InsertDistrict(name);
                                break;
                            }
                        case 1:
                            {
                                InsertCity(name);
                                break;
                            }
                    }
                }
            }
        }

        private void удалитьToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (treeView.SelectedNode != null)
            {
                using (var conn = new SqlConnection(connectionString))
                {
                    conn.Open();
                    string sql = "";
                    string value = "";
                    switch (treeView.SelectedNode.Level)
                    {
                        case 0:
                            {
                                sql = "delete from RegionInfo where regionCode = @regionCode";
                                value = "@regionCode";
                                break;
                            }
                        case 1:
                            {
                                sql = "delete from DistrictInfo where districtCode = @districtCode";
                                value = "@districtCode";
                                break;
                            }
                        case 2:
                            {
                                sql = "delete from CityInfo where cityCode = @cityCode";
                                value = "@cityCode";
                                break;
                            }

                    }
                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue(value, treeView.SelectedNode.Tag);
                    cmd.ExecuteNonQuery();
                    treeView.SelectedNode.Remove();
                }
            }
        }

        private void редактироватьToolStripMenuItem_Click(object sender, EventArgs e)
        { 
            TreeNode node = treeView.SelectedNode;
            CreateUpdateForm createUpdateForm = null;
            string[] table = null;
            switch (node.Level)
            {
                case 0:
                    {
                        createUpdateForm = new CreateUpdateForm(node, new List<TreeNode>(), null);
                        table = new string[3] { "RegionInfo", "region", "" };
                        break;
                    }
                case 1:
                    {
                        createUpdateForm = new CreateUpdateForm(node, zeroLevelNodes, treeView.SelectedNode.Parent);
                        table = new string[3] { "DistrictInfo", "district", "region" };
                        break;
                    }
                case 2:
                    {
                        createUpdateForm = new CreateUpdateForm(node, firstLevelNodes, treeView.SelectedNode.Parent);
                        table = new string[3] { "CityInfo", "city", "district" };
                        break;
                    }
            }

            createUpdateForm.ShowDialog();

            if (treeView.SelectedNode.Text != createUpdateForm.name)
                { UpdateName(treeView.SelectedNode, createUpdateForm.name, table); }
            
            if (createUpdateForm.parent != null)
            {
                if (treeView.SelectedNode.Text != createUpdateForm.parent.Text)
                    { ChangeDirectory(treeView.SelectedNode, createUpdateForm.parent, table); }
            }
        }


        private void UpdateName(TreeNode selectedNode, string name, string[] table)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = String.Format("update {0} set {1} = '{2}' where {3} = {4}", 
                    table[0], table[1] + "Name", name, table[1] + "Code", (short)selectedNode.Tag);
                var cmd = new SqlCommand(sql, conn);

                try { cmd.ExecuteNonQuery(); }
                catch (Exception ex) { MessageBox.Show(ex.Message); }

                treeView.Nodes.Clear();
                LoadRegions();
            }
        }

        private void ChangeDirectory(TreeNode current, TreeNode parent, string[] table)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = String.Format("update {0} set {1} = {2} where {3} = {4}",
                    table[0], table[2] + "Code", (short)parent.Tag, table[1] + "Code", (short)current.Tag);

                var cmd = new SqlCommand(sql, conn);

                try { cmd.ExecuteNonQuery(); }
                catch (Exception ex) { MessageBox.Show(ex.Message); }

                treeView.Nodes.Clear();
                LoadRegions();
            }
        }


        private void InsertRegion(string name)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"insert into RegionInfo (regionName) values (@name)";

                try
                {
                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@regionCode", treeView.SelectedNode.Tag);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                    LoadRegions();
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void InsertDistrict(string name)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"insert into DistrictInfo (districtName, regionCode) values (@name, @tag)";

                try
                {
                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@tag", treeView.SelectedNode.Tag);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                    LoadDistricts((short)treeView.SelectedNode.Tag, treeView.SelectedNode);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }

        private void InsertCity(string name)
        {
            using (var conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"insert into CityInfo (cityName, districtCode) values (@name, @tag)";

                try
                {
                    var cmd = new SqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("@tag", treeView.SelectedNode.Tag);
                    cmd.Parameters.AddWithValue("@name", name);
                    cmd.ExecuteNonQuery();
                    LoadCities((short)treeView.SelectedNode.Tag, treeView.SelectedNode);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message); }
            }
        }


        private void treeView_DoubleClick(object sender, EventArgs e)
        {
            TreeNode treeNode = treeView.SelectedNode;
            switch (treeNode.Level)
            {
                case 0:
                    treeNode.Nodes.Clear();
                    LoadDistricts((short)treeNode.Tag, treeNode);
                    treeNode.Expand();
                    break;
                case 1:
                    treeNode.Nodes.Clear();
                    LoadCities((short)treeNode.Tag, treeNode);
                    treeNode.Expand();
                    break;
                case 2:
                    break;
            }
        }
    }
}

