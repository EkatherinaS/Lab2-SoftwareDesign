using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Lab2
{
    public partial class CreateUpdateForm : Form
    {
        public string name;
        public TreeNode parent;

        //Insert
        public CreateUpdateForm(TreeNode treeNode)
        {
            InitializeComponent();
            name = null;
            try 
            {
                parentComboBox.Items.Add(treeNode.Text);
                parentComboBox.Tag = treeNode.Tag;
                parentComboBox.SelectedIndex = 0;
            }
            catch (NullReferenceException)
            {
                parentComboBox.Text = null;
            }
            parentComboBox.Enabled = false;
        }

        //Update
        public CreateUpdateForm(TreeNode treeNode, List<TreeNode> levelNodes, TreeNode parent)
        {
            InitializeComponent();
            NameTextbox.Text = treeNode.Text;
            name = treeNode.Text;
            parentComboBox.Enabled = true;

            if (parent == null)
            {
                parentComboBox.Text = "";
                parentComboBox.Enabled = false; 
            }
            else { parentComboBox.Text = parent.Text; }

            foreach (TreeNode n in levelNodes)
            {
                parentComboBox.Items.Add(n);
            }
        }


        private void button1_Click(object sender, EventArgs e)
        {
            name = NameTextbox.Text;
            parent = parentComboBox.SelectedItem as TreeNode;
            Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Close();
        }
    }
}
