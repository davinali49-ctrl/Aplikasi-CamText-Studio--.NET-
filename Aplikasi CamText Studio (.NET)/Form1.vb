Imports AForge.Video
Imports AForge.Video.DirectShow
Imports System.Drawing
Imports System.Windows.Forms

Public Class Form1
    Dim videoDevices As FilterInfoCollection
    Dim finalVideo As VideoCaptureDevice
    Dim capturedImage As Bitmap

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' 1. Deteksi webcam
        Try
            videoDevices = New FilterInfoCollection(FilterCategory.VideoInputDevice)
            For Each device As FilterInfo In videoDevices
                ComboBox1.Items.Add(device.Name)
            Next
            If ComboBox1.Items.Count > 0 Then ComboBox1.SelectedIndex = 0
        Catch ex As Exception
            MessageBox.Show("Gagal mendeteksi webcam: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try

        ' 2. SETUP LABEL1 TRANSPARAN DI ATAS PICTUREBOX1
        ' Ubah Parent Label1 ke PictureBox1 agar transparansinya menembus foto, bukan Form
        Label1.Parent = PictureBox1
        Label1.BackColor = Color.Transparent
        Label1.ForeColor = Color.White ' Warna teks (ubah ke Color.Yellow / Color.Red sesuai selera)
        Label1.Font = New Font("Segoe UI", 16, FontStyle.Bold)
        Label1.Location = New Point(15, 15) ' Posisi pojok kiri atas di dalam PictureBox
        Label1.Text = "" ' Kosongkan teks bawaan "Label1" saat aplikasi baru jalan

        ' 3. Hubungkan Event Click ke Tombol
        AddHandler btnStart.Click, AddressOf btnStart_Click
        AddHandler btnCapture.Click, AddressOf btnCapture_Click
        AddHandler btnAddText.Click, AddressOf btnAddText_Click

        If Controls.ContainsKey("btnSave") Then
            Dim btn As Control = Controls("btnSave")
            btn.BringToFront()
            AddHandler btn.Click, AddressOf btnSave_Click
        End If
    End Sub

    ' 1. Mulai Kamera (btnStart)
    Private Sub btnStart_Click(sender As Object, e As EventArgs)
        If videoDevices IsNot Nothing AndAlso videoDevices.Count > 0 Then
            If finalVideo IsNot Nothing AndAlso finalVideo.IsRunning Then
                finalVideo.SignalToStop()
            End If

            ' Bersihkan teks label saat mulai kamera baru
            Label1.Text = ""
            capturedImage = Nothing

            finalVideo = New VideoCaptureDevice(videoDevices(ComboBox1.SelectedIndex).MonikerString)
            AddHandler finalVideo.NewFrame, AddressOf NewFrame_Event
            finalVideo.Start()
        End If
    End Sub

    ' Frame dari kamera ditampilkan di PictureBox
    Private Sub NewFrame_Event(sender As Object, eventArgs As NewFrameEventArgs)
        Dim bmp As Bitmap = CType(eventArgs.Frame.Clone(), Bitmap)
        bmp.RotateFlip(RotateFlipType.RotateNoneFlipX) ' Anti-mirror
        PictureBox1.Image = bmp
    End Sub

    ' 2. Ambil Foto dari Webcam (btnCapture)
    Private Sub btnCapture_Click(sender As Object, e As EventArgs)
        If finalVideo IsNot Nothing AndAlso finalVideo.IsRunning Then
            finalVideo.SignalToStop()

            If PictureBox1.Image IsNot Nothing Then
                capturedImage = New Bitmap(PictureBox1.Image)
                PictureBox1.Image = capturedImage
                Label1.Text = "" ' Reset teks overlay saat foto baru diambil
            End If
        End If
    End Sub

    ' 3. Menambahkan Teks ke Label1 Transparan (btnAddText)
    Private Sub btnAddText_Click(sender As Object, e As EventArgs)
        If Not String.IsNullOrWhiteSpace(TextBox1.Text) Then
            ' Jika Label1 masih kosong, isi teks pertama. Jika sudah ada isi, tambah baris baru di bawahnya
            If String.IsNullOrEmpty(Label1.Text) Then
                Label1.Text = TextBox1.Text
            Else
                Label1.Text &= Environment.NewLine & TextBox1.Text
            End If

            TextBox1.Clear()
            TextBox1.Focus()
        End If
    End Sub

    ' 4. Simpan Foto beserta Teks Overlay dari Label1 (btnSave)
    Private Sub btnSave_Click(sender As Object, e As EventArgs)
        If PictureBox1.Image IsNot Nothing AndAlso capturedImage IsNot Nothing Then
            Dim sfd As New SaveFileDialog()
            sfd.Filter = "JPEG Image (*.jpg)|*.jpg|PNG Image (*.png)|*.png"
            sfd.Title = "Simpan Hasil Foto"
            sfd.FileName = "Webcam_Photo_" & DateTime.Now.ToString("yyyyMMdd_HHmmss")

            If sfd.ShowDialog() = DialogResult.OK Then
                Try
                    ' Buat salinan bitmap agar foto asli tetap bersih jika user masih mau edit teks lagi
                    Using finalToSave As New Bitmap(capturedImage)
                        If Not String.IsNullOrEmpty(Label1.Text) Then
                            Using g As Graphics = Graphics.FromImage(finalToSave)
                                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias

                                ' Sesuaikan skala font dari tampilan layar ke resolusi asli foto webcam
                                Dim scaleRatio As Single = CSng(finalToSave.Width) / CSng(PictureBox1.Width)
                                Dim scaledFontSize As Single = Label1.Font.Size * scaleRatio
                                Dim renderFont As New Font(Label1.Font.FontFamily, scaledFontSize, Label1.Font.Style)

                                Dim xPos As Single = Label1.Left * scaleRatio
                                Dim yPos As Single = Label1.Top * scaleRatio

                                ' Gambar bayangan hitam & teks utama sesuai warna Label1
                                g.DrawString(Label1.Text, renderFont, Brushes.Black, xPos + (3 * scaleRatio), yPos + (3 * scaleRatio))
                                Using textBrush As New SolidBrush(Label1.ForeColor)
                                    g.DrawString(Label1.Text, renderFont, textBrush, xPos, yPos)
                                End Using
                            End Using
                        End If

                        If sfd.FilterIndex = 1 Then
                            finalToSave.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Jpeg)
                        Else
                            finalToSave.Save(sfd.FileName, System.Drawing.Imaging.ImageFormat.Png)
                        End If
                    End Using

                    MessageBox.Show("Foto dan teks berhasil disimpan!", "Sukses", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Catch ex As Exception
                    MessageBox.Show("Gagal menyimpan foto: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
                End Try
            End If
        Else
            MessageBox.Show("Belum ada foto yang diambil! Klik Start lalu Capture terlebih dahulu.", "Peringatan", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub

    ' Hentikan kamera saat aplikasi ditutup
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If finalVideo IsNot Nothing AndAlso finalVideo.IsRunning Then
            finalVideo.SignalToStop()
        End If
    End Sub

    Private Sub btnCapture_Click_1(sender As Object, e As EventArgs) Handles btnCapture.Click

    End Sub

    ' Tambahkan di bagian deklarasi variabel atas (di bawah Dim capturedImage As Bitmap)
    Dim isDragging As Boolean = False
    Dim dragOffset As Point

    ' Tambahkan 3 Sub ini di bagian bawah dalam Class Form1:
    Private Sub Label1_MouseDown(sender As Object, e As MouseEventArgs) Handles Label1.MouseDown
        If e.Button = MouseButtons.Left Then
            isDragging = True
            dragOffset = e.Location
            Label1.Cursor = Cursors.SizeAll ' Ubah kursor jadi tanda panah geser
        End If
    End Sub

    Private Sub Label1_MouseMove(sender As Object, e As MouseEventArgs) Handles Label1.MouseMove
        If isDragging Then
            ' Hitung posisi baru dan batasi agar teks tidak keluar dari area PictureBox1
            Dim newX As Integer = Math.Max(0, Math.Min(PictureBox1.Width - Label1.Width, Label1.Left + e.X - dragOffset.X))
            Dim newY As Integer = Math.Max(0, Math.Min(PictureBox1.Height - Label1.Height, Label1.Top + e.Y - dragOffset.Y))
            Label1.Location = New Point(newX, newY)
        End If
    End Sub

    Private Sub Label1_MouseUp(sender As Object, e As MouseEventArgs) Handles Label1.MouseUp
        isDragging = False
        Label1.Cursor = Cursors.Default
    End Sub
End Class